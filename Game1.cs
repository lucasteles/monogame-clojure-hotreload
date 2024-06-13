using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class Game1 : Game
{
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    ClojureEngine clojureEngine;

    public Game1()
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        spriteBatch = new(GraphicsDevice);
        clojureEngine = new(this, graphics, spriteBatch);
        clojureEngine.Initialize();
        base.Initialize();
    }

    protected override void LoadContent() =>
        clojureEngine.LoadContent();

    protected override void Update(GameTime gameTime)
    {
        clojureEngine.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        clojureEngine.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        clojureEngine.Dispose();
        base.UnloadContent();
    }
}
