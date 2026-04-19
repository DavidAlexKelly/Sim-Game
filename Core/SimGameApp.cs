using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimGame.Entities;
using SimGame.Rendering;
using SimGame.UI;

namespace SimGame.Core
{
    public class SimGameApp : Game
    {
        private const int WorldWidth     = 128;
        private const int WorldHeight    = 128;
        private const int TileSize       = 16;
        private const int CharacterCount = 20;

        // How close (pixels) a click must be to a character centre to select it
        private const float CharacterPickRadius = TileSize * 0.8f;

        private readonly GraphicsDeviceManager _graphics;

        private SpriteBatch?    _spriteBatch;
        private Renderer?       _renderer;
        private Camera?         _camera;
        private TickSystem?     _ticks;
        private InputHandler?   _input;
        private EntityManager?  _entities;
        private World.World?    _world;
        private DebugHud?       _hud;
        private InfoPanel?      _infoPanel;
        private SelectionState  _selection = new();

        public SimGameApp()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth  = 1280,
                PreferredBackBufferHeight = 720
            };
            Content.RootDirectory = "Content";
            IsMouseVisible        = true;
        }

        protected override void Initialize()
        {
            Window.Title = "SimGame";
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            var font     = Content.Load<SpriteFont>("Arial22");

            _input     = new InputHandler();
            _ticks     = new TickSystem();
            _renderer  = new Renderer(GraphicsDevice, TileSize);
            _camera    = new Camera(GraphicsDevice);
            _entities  = new EntityManager(TileSize);
            _hud       = new DebugHud(_spriteBatch, font);
            _infoPanel = new InfoPanel(_spriteBatch, font, GraphicsDevice);

            LoadWorld();
        }

        private void LoadWorld(int seed = 0)
        {
            _selection.Clear();
            _world = new World.World(WorldWidth, WorldHeight, seed);
            _entities!.SpawnCharacters(CharacterCount, _world);
            _renderer!.BakeWorld(_world);

            _camera!.CentreOn(
                WorldWidth  * TileSize * 0.5f,
                WorldHeight * TileSize * 0.5f);
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _input!.Update();

            if (_input.JustPressed(Keys.Escape)) Exit();
            if (_input.JustPressed(Keys.Space))  _ticks!.TogglePause();
            if (_input.JustPressed(Keys.R))       LoadWorld();
            if (_input.JustPressed(Keys.D1))      _ticks!.SetSpeed(0.5f);
            if (_input.JustPressed(Keys.D2))      _ticks!.SetSpeed(1f);
            if (_input.JustPressed(Keys.D3))      _ticks!.SetSpeed(3f);
            if (_input.JustPressed(Keys.D4))      _ticks!.SetSpeed(8f);

            _camera!.Update(dt, _input.State);

            if (_input.JustClicked())
                HandleClick(_input.MouseScreenPos);

            int tickCount = _ticks!.Update(dt);
            for (int i = 0; i < tickCount; i++)
                _entities!.Tick(_world!, _renderer!);

            _entities!.UpdateRenderPositions(dt, _ticks.Paused ? 0f : _ticks.SpeedMultiplier);

            base.Update(gameTime);
        }

        private void HandleClick(Vector2 screenPos)
        {
            Vector2 worldPos = _camera!.ScreenToWorld(screenPos);

            // 1. Check characters first (they sit on top of tiles)
            float bestDist = CharacterPickRadius;
            Entities.Character? picked = null;

            foreach (var c in _entities!.Characters)
            {
                float dist = Vector2.Distance(c.Position, worldPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    picked   = c;
                }
            }

            if (picked != null)
            {
                _selection.SelectCharacter(picked);
                return;
            }

            // 2. Fall back to tile
            int tx = (int)(worldPos.X / TileSize);
            int ty = (int)(worldPos.Y / TileSize);

            if (_world!.InBounds(tx, ty))
                _selection.SelectTile(tx, ty);
            else
                _selection.Clear();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(15, 15, 15));

            _renderer!.Draw(_entities!.Characters, _camera!);
            _hud!.Draw(_ticks!, _entities.Characters, _world!.Seed);
            _infoPanel!.Draw(_selection, _world, GraphicsDevice);

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _renderer?.Dispose();
            _infoPanel?.Dispose();
            base.UnloadContent();
        }
    }
}