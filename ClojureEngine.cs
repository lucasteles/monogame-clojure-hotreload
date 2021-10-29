using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using clojure.lang;
using clojure.clr.api;

public class ClojureEngine : IDisposable
{
    readonly Game game;
    readonly GraphicsDeviceManager graphics;
    readonly SpriteBatch spriteBatch;

    SpriteFont errorFont;
    IFn cljInitialize, cljLoadContent, cljUpdate, cljDraw;
    string cljSrc;

    FileSystemWatcher watcher = new();
    Exception currentError;
    bool showedError,  forceReload;

    public ClojureEngine(Game game, GraphicsDeviceManager graphics, SpriteBatch spriteBatch)
    {
        this.game = game;
        this.graphics = graphics;
        this.spriteBatch = spriteBatch;
        cljSrc = Path.Combine(GetProjectPath(), "cljgame");
        ConfigureWatcher();
    }

    bool ShouldWait() => currentError is not null;
    void ConfigureWatcher()
    {
        watcher.Filter = "*.*";
        watcher.Path = cljSrc;
        watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Created += WatcherHandler;
        watcher.Deleted += WatcherHandler;
        watcher.Renamed += WatcherHandler;
        watcher.Changed += WatcherHandler;
    }

    void WatcherHandler(object sender, FileSystemEventArgs e) => forceReload = true;

    public void Initialize()
    {
        try
        {
            LoadSymbols();
        }
        catch (Exception e)
        {
            currentError = e;
        }
    }

    void LoadSymbols()
    {
        var load = Clojure.var("clojure.core", "load");
        load.invoke("/cljgame/game");
        IFn loadFn(string fnName) => Clojure.var("cljgame.game", fnName);
        cljInitialize = loadFn("Initialize");
        cljLoadContent = loadFn("LoadContent");
        cljUpdate = loadFn("Update");
        cljDraw = loadFn("Draw");
        cljInitialize?.invoke(game,spriteBatch, graphics, game.GraphicsDevice, game.Window);
    }
    public void LoadContent()
    {
        errorFont = game.Content.Load<SpriteFont>("arialfont");
        try
        {
            cljLoadContent?.invoke(game);

        }
        catch (Exception e)
        {
            currentError = e;
        }
    }

    public void Update(GameTime gameTime)
    {
        if (forceReload)
        {
            Console.WriteLine("RELOAD FORCED");
            currentError = null;
            showedError = forceReload = false;
            UpdateCljFiles();
            LoadSymbols();
            LoadContent();
            return;
        }

        if (ShouldWait()) return;
        try
        {
            cljUpdate?.invoke(game, gameTime);
        }
        catch (Exception e)
        {
            currentError = e;
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (currentError is not null) DrawErrorScreen();
        if (ShouldWait()) return;

        try
        {
            cljDraw?.invoke(game, gameTime);
        }
        catch (Exception e)
        {
            currentError = e;
        }
    }

    static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }

    void UpdateCljFiles()
    {
        var current = Directory.GetCurrentDirectory();
        var output = Path.Combine(current, "cljgame");
        Directory.Delete(output, true);
        CopyFilesRecursively(cljSrc, output);
    }
    void DrawErrorScreen()
    {
        var exn = currentError;
        if (exn is null)
            return;

        game.GraphicsDevice.Clear(Color.Black);

        var exStr = $"{exn}\nInnerException:{exn.InnerException}";
        if (!showedError)
        {
            Console.WriteLine(exn);
            showedError = true;
        }
        var error =
            string.Join("\n",
                exStr.Select((c, index) => new {c, index})
                    .GroupBy(x => x.index/100)
                    .Select(group => group.Select(elem => elem.c))
                    .Select(chars => new string(chars.ToArray())));

        spriteBatch.Begin();
        spriteBatch.DrawString(errorFont, error, Vector2.Zero, Color.White);
        spriteBatch.End();
    }

    string GetProjectPath(string path = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            path = Directory.GetCurrentDirectory();

        return Directory.GetFiles(path, "*.csproj").Any()
            ? path
            : GetProjectPath(Directory.GetParent(path)?.FullName);
    }

    public void Dispose()
    {
        watcher.Created -= WatcherHandler;
        watcher.Deleted -= WatcherHandler;
        watcher.Changed -= WatcherHandler;
        watcher.Renamed -= WatcherHandler;
    }
}