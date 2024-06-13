using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using clojure.lang;
using clojure.clr.api;

public sealed class ClojureEngine : IDisposable
{
    readonly Game game;
    readonly GraphicsDeviceManager graphics;
    readonly SpriteBatch spriteBatch;

    SpriteFont errorFont;
    IFn cljInitialize, cljLoadContent, cljUpdate, cljDraw;

    readonly string cljSrc;
    readonly IFn load;
    readonly FileSystemWatcher watcher = new();
    readonly HashSet<string> filesToReload = [];

    bool errorShowed, forceReload;
    Exception currentError;

    const string ScriptDir = "cljgame";

    public ClojureEngine(Game game, GraphicsDeviceManager graphics, SpriteBatch spriteBatch)
    {
        this.game = game;
        this.graphics = graphics;
        this.spriteBatch = spriteBatch;
#if DEBUG
        cljSrc = Path.Combine(GetGameSourcePath(), ScriptDir);
#else
        cljSrc = ScriptDir;
#endif
        ConfigureWatcher();
        load = Clojure.var("clojure.core", "load");
    }

    bool ShouldWait() => currentError is not null;

    void ConfigureWatcher()
    {
        watcher.Filter = "*.*";
        watcher.Path = cljSrc;
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.Created += WatcherHandler;
        watcher.Deleted += WatcherHandler;
        watcher.Renamed += WatcherHandler;
        watcher.Changed += WatcherHandler;
    }

    void WatcherHandler(object sender, FileSystemEventArgs e)
    {
        if (e.Name is null || File.GetAttributes(e.FullPath.TrimEnd('~'))
                .HasFlag(FileAttributes.Directory))
            return;

        if (filesToReload.Add(e.Name))
            forceReload = true;
    }

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

    static IFn LoadFn(string fnName) => Clojure.var($"{ScriptDir}.game", fnName);

    void LoadSymbols()
    {
        load.invoke($"/{ScriptDir}/game");
        cljInitialize = LoadFn("Initialize");
        cljLoadContent = LoadFn("LoadContent");
        cljUpdate = LoadFn("Update");
        cljDraw = LoadFn("Draw");
        cljInitialize?.invoke(game, spriteBatch, graphics, game.GraphicsDevice, game.Window);
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
        try
        {
            if (forceReload)
            {
                Console.WriteLine("Reloading game...");
                currentError = null;
                errorShowed = forceReload = false;
                UpdateCljFiles();
                ReloadChangedFiles();
                LoadSymbols();
                LoadContent();
                return;
            }

            if (ShouldWait()) return;
            cljUpdate?.invoke(game, gameTime);
        }
        catch (Exception e)
        {
            currentError = e;
        }
    }

    void ReloadChangedFiles()
    {
        foreach (var file in filesToReload)
        {
            var filepath = Path.Combine(
                Path.GetDirectoryName(file) ?? string.Empty,
                Path.GetFileNameWithoutExtension(file)
            );
            var name = Path.Combine($"/{ScriptDir}", filepath);
            Console.WriteLine($"Reloading {name}");

            try
            {
                load.invoke(name);
            }
            catch (Exception e)
            {
                currentError = e;
            }
            finally
            {
                filesToReload.Remove(file);
            }
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
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*",
                     SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }

    void UpdateCljFiles()
    {
        var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                      ?? throw new InvalidOperationException("Null executing assembly location");

        var output = Path.Combine(current, ScriptDir);
        if (cljSrc == output) return;
        Directory.Delete(output, true);
        Directory.CreateDirectory(output);
        CopyFilesRecursively(cljSrc, output);
    }

    void DrawErrorScreen()
    {
        var exn = currentError;
        if (exn is null)
            return;

        game.GraphicsDevice.Clear(Color.Black);

        var exStr = $"{exn}\nInnerException:{exn.InnerException}";
        if (!errorShowed)
        {
            Console.WriteLine(exn);
            errorShowed = true;
            try
            {
                spriteBatch.End();
            }
            catch
            {
                // SKIP
            }
        }

        var error = string.Join('\n',
            exStr.Select((c, index) => (c, index))
                .GroupBy(x => x.index / 100)
                .Select(group => group.Select(elem => elem.c))
                .Select(chars => new string(chars.ToArray())));

        spriteBatch.Begin();
        spriteBatch.DrawString(errorFont, error, Vector2.Zero, Color.White);
        spriteBatch.End();
    }

    static string GetGameSourcePath(string path = null)
    {
        for (var i = 0; i < 100; i++)
        {
            if (string.IsNullOrWhiteSpace(path)) path = Directory.GetCurrentDirectory();
            if (Directory.GetFiles(path, "*.csproj").Any()) return path;
            path = Directory.GetParent(path)?.FullName;
        }

        throw new InvalidOperationException("Unable to find project directory");
    }

    public void Dispose()
    {
        watcher.Created -= WatcherHandler;
        watcher.Deleted -= WatcherHandler;
        watcher.Changed -= WatcherHandler;
        watcher.Renamed -= WatcherHandler;
    }
}
