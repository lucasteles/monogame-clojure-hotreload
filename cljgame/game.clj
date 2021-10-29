(ns cljgame.game
  (:require [cljgame.interop :refer [current current-exe-dir int32 load-monogame]]
            [cljgame.monogame :as g]
            [cljgame.state :as state])

  (:import [System Console]
           [System.IO Path])
  (:gen-class))

(load-monogame)

(import [Microsoft.Xna.Framework Color Vector2]
        [Microsoft.Xna.Framework.Input Keyboard Keys]
        [Microsoft.Xna.Framework.Graphics SpriteEffects])

(defn init [game { graphics :graphics-manager
                   window :window }]
  (println "init in" game window)
  (set! (.IsMouseVisible game) true)
  (set! (.PreferredBackBufferWidth graphics) (int32 1024))
  (set! (.PreferredBackBufferHeight graphics) (int32 768))
  (.ApplyChanges graphics)

  {:rotation 0
   :position  (g/vect (-> window g/width (/ 2))
                      (-> window g/height (/ 2))) })

(defn read-keys [game]
  (let [keyboard (Keyboard/GetState)
        pressed (fn [k] (.IsKeyDown keyboard k))]
    (cond 
      (and (pressed Keys/W) (pressed Keys/A)) (g/vect -2 -2)
      (and (pressed Keys/W) (pressed Keys/D)) (g/vect 2 -2)
      (and (pressed Keys/S) (pressed Keys/A)) (g/vect -2 2)
      (and (pressed Keys/S) (pressed Keys/D)) (g/vect 2 2)
      (pressed Keys/W) (g/vect 0 -2)
      (pressed Keys/S) (g/vect 0 2)
      (pressed Keys/A) (g/vect -2 0)
      (pressed Keys/D) (g/vect 2 0)
      (pressed Keys/Escape (g/exit game))
      :else Vector2/Zero)))

(defn load-content [game {state :state}]
  (assoc state
         :texture/logo
         (g/load-texture-2d game "logo")))

(defn update- [{:keys [game game-time state]}]
  (let [{rot :rotation position :position} state
        velocity (read-keys)]
    (assoc state
           :rotation (+ rot 0.01) 
           :position (g/vect+ position velocity))))

(defn draw [{:keys [sprite-batch delta-time graphics-device]
             { logo :texture/logo
              rotation :rotation
              position :position } :state}]
  (let [logo-center (g/vect (-> logo .Bounds .Width (/ 2))
                            (-> logo .Bounds .Height (/ 2)))]
    (g/clear graphics-device Color/LightGray)

    (g/begin sprite-batch)
    (g/draw sprite-batch {:texture logo
                          :position position
                          :source-rectangle (.Bounds logo)
                          :color Color/White
                          :rotation rotation
                          :origin logo-center
                          :scale 0.5
                          :effects SpriteEffects/None
                          :layer-depth 0})
    (g/end sprite-batch)))

(Console/WriteLine "Ola Delboni")
(println "Monogame on Clojure")
;; called from C#
(def Initialize (partial state/Initialize init))
(def LoadContent (partial state/LoadContent load-content))
(def Update (partial state/Update update-))
(def Draw (partial state/Draw draw))