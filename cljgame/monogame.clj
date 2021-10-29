(ns cljgame.monogame
  (:require [cljgame.interop :refer [current-exe-dir get-prop load-monogame]])
  (:import [System.IO Directory Path Directory]
           [System.Linq Enumerable]))


(import [Microsoft.Xna.Framework Game GraphicsDeviceManager Color Vector2 Rectangle GameWindow]
        [Microsoft.Xna.Framework.Graphics SpriteBatch Texture2D SpriteSortMode SpriteEffects SpriteFont]
        [Microsoft.Xna.Framework.Content ContentManager])


(load-monogame)

(def graphics-device (fn [game] (get-prop game "GraphicsDevice")))

(defn clear [graphics-device color]
  (.Clear graphics-device Color/LightGray))

(defn begin [sprite-batch &{:keys [sort-mode blend-state sampler-state depthStencil-state rasterizer-state effect transform-matrix]}]
  (.Begin sprite-batch
          (or sort-mode SpriteSortMode/Deferred)
          blend-state
          sampler-state
          depthStencil-state
          rasterizer-state
          effect
          transform-matrix))

(defn end [sprite-batch] (.End sprite-batch))

(defn draw [sprite-batch {:keys [texture position source-rectangle color rotation origin scale effects layer-depth
                                 destination-rectangle]}]
  (cond
    (and texture position source-rectangle color rotation origin scale effects layer-depth)
    (.Draw sprite-batch texture position source-rectangle color rotation origin scale effects layer-depth)

    (and  texture destination-rectangle source-rectangle color rotation origin effects layer-depth)
    (.Draw sprite-batch texture destination-rectangle source-rectangle color rotation origin effects layer-depth)

    (and texture position source-rectangle color)
    (.Draw sprite-batch texture position source-rectangle color)

    (and texture destination-rectangle source-rectangle color)
    (.Draw sprite-batch texture destination-rectangle source-rectangle color)

    (and texture destination-rectangle color)
    (.Draw sprite-batch texture destination-rectangle color)

    (and texture position color)
    (.Draw sprite-batch texture position color)
    
    :else
    (throw (new Exception "INVALID DRAW PARAMETERS"))))

(defn draw-text [sprite-batch {:keys [sprite-font text position color
                                      rotation origin scale effects layer-depth]}]
  (cond 
    (and sprite-font text position color)   
    (.DrawString sprite-batch sprite-font (str text) position color)

    (and sprite-font text position color rotation origin scale effects layer-depth)
    (.DrawString sprite-batch sprite-font (str text) position color rotation origin scale effects layer-depth)

    :else
    (throw (new Exception "INVALID DRAW TEXT PARAMETERS"))))


(defn load-texture-2d [game texture-name]
  (let [content (get-prop game "Content")]
    (.Load ^ContentManager content (type-args Texture2D) texture-name)))

(defn load-sprite-font [game font-name] 
  (let [content (get-prop game "Content")]
    (.Load ^ContentManager content (type-args SpriteFont) font-name)))

(defn pixel-texture [game color]
  (let [graphics (graphics-device game)
        texture (new Texture2D graphics 1 1)
        color-array (Enumerable/ToArray (type-args Color) (Enumerable/Cast (type-args Color) [color]))]
    (.SetData ^Texture2D texture (type-args Color) color-array)
    texture))

(defn vect
  ([n] (new Vector2 n))
  ([x y] (new Vector2 x y)))

(defn vect+ [v1 v2] (Vector2/op_Addition v1 v2))
(defn vect- 
  ([v] (Vector2/op_UnaryNegation v))
  ([v1 v2] (Vector2/op_Subtraction v1 v2)))

(defn vect* [^Vector2 a b] (Vector2/op_Multiply a b))
(defn vect-div [^Vector2 a b] (Vector2/op_Division a b))
(defn vect-map [^Vector2 v] { :x (.X v) :y (.Y v)})
(defn vect-with-x [^Vector2 v x] (vect x (.Y v)))
(defn vect-with-y [^Vector2 v y] (vect (.X v) y))

(defn rect [^Vector2 location ^Vector2 size] 
  (new Rectangle (.ToPoint location) (.ToPoint size)) )
(defn rect-intersects [^Rectangle r1 ^Rectangle r2] (.Intersects r1 r2))
(defn width [^GameWindow window] (-> window .ClientBounds .Width))
(defn height [^GameWindow window] (-> window .ClientBounds .Height))
(defn window-size [^GameWindow window] (let [bounds (.ClientBounds window)] (vect (.Width bounds) (.Height bounds))))
(defn tap [v] (println v) v)

