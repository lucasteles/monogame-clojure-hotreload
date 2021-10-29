﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Physics2D.Dynamics;
using tainicom.Aether.Physics2D;
public class Game1 : Game
{
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    ClojureEngine clojureEngine;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        _ = new Body(); // loads the assembly to be used inside clj
    }

    protected override void Initialize()
    {
        spriteBatch = new(GraphicsDevice);
        clojureEngine = new(this, graphics, spriteBatch);
        clojureEngine.Initialize();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        clojureEngine.LoadContent();
    }

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