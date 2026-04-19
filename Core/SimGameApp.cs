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
        // ── Config ───────────────────────────────────────────────────────────
        private const int WorldWidth      = 128;
        private const int WorldHeight     = 128;
        private const int TileSize        = 16;
        private const int CharacterCount  = 20;
        // ────────────────────────────────────────────────────────────────────

        private readonly GraphicsDeviceManager _graphics;

        private SpriteBatch?   _spriteBatch;
        private Renderer?      _renderer;
        private Camera?        _camera;
        private TickSystem?    _ticks;
        private InputHandler?  _input;
        private EntityManager? _entities;
        private World.World?   _world;
        private DebugHud?      _hud;

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

            _input    = new InputHandler();
            _ticks    = new TickSystem(tickIntervalSeconds: 0.20f);
            _renderer = new Renderer(GraphicsDevice, TileSize);
            _camera   = new Camera(GraphicsDevice);
            _entities = new EntityManager(TileSize);
            _hud      = new DebugHud(_spriteBatch, font);

            LoadWorld();
        }

        private void LoadWorld(int seed = 0)
        {
            _world = new World.World(WorldWidth, WorldHeight, seed);
            _entities!.SpawnCharacters(CharacterCount, _world);
            _renderer!.BakeWorld(_world);

            // Centre camera on the world
            _camera!.CentreOn(
                WorldWidth  * TileSize * 0.5f,
                WorldHeight * TileSize * 0.5f);
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _input!.Update();

            // ── One-shot actions ─────────────────────────────────────────────
            if (_input.JustPressed(Keys.Escape)) Exit();
            if (_input.JustPressed(Keys.Space))  _ticks!.TogglePause();
            if (_input.JustPressed(Keys.R))       LoadWorld();         // new random seed
            if (_input.JustPressed(Keys.D1))      _ticks!.SetSpeed(0.5f);
            if (_input.JustPressed(Keys.D2))      _ticks!.SetSpeed(1f);
            if (_input.JustPressed(Keys.D3))      _ticks!.SetSpeed(3f);
            if (_input.JustPressed(Keys.D4))      _ticks!.SetSpeed(8f);

            // ── Camera (uses held keys internally) ───────────────────────────
            _camera!.Update(dt, _input.State);

            // ── Sim ticks ────────────────────────────────────────────────────
            int tickCount = _ticks!.Update(dt);
            for (int i = 0; i < tickCount; i++)
                _entities!.Tick(_world!);

            // ── Visual lerp (every frame) ────────────────────────────────────
            _entities!.UpdateRenderPositions(dt);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(15, 15, 15));

            _renderer!.Draw(_entities!.Characters, _camera!);
            _hud!.Draw(_ticks!, _entities.Characters.Count, _world!.Seed);

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _renderer?.Dispose();
            base.UnloadContent();
        }
    }
}
