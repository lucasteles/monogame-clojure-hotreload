(ns cljgame.game
  (:require [cljgame.monogame :as g]
            [cljgame.state :as state]
            [cljgame.physics :as physics]
            [cljgame.entities.floor :as floor]
            [cljgame.entities.pipes :as pipes]
            [cljgame.entities.bird :as bird]
            [cljgame.entities.score :as score]
            [cljgame.entities.gameover :as gameover]
            [cljgame.entities.background :as background])
  (:import [System Console]))

(defn game-configuration! [game graphics]
  (g/set-mouse-visible game true)
  (g/set-screen-size graphics {:width 1024 :height 768})
  (g/apply-changes graphics))

(defn exit-on-esc [game keyboard-state]
  (when (g/is-key-dowm keyboard-state :escape)
        (g/exit game)))

(defn handle-pause [state keyboard-state]
  (if (g/is-key-dowm keyboard-state :space)
    (assoc state :paused false) state))

(defn initialize [game { graphics :graphics-manager window :window  update-state! :update-state!  }]
  (let [world (physics/create-world (g/vect 0 20))]
    (game-configuration! game graphics)

    {:world world
     :floor (floor/init game window world)
     :background (background/init game window)
     :bird (bird/init game world update-state!)
     :pipe-manager (pipes/init game)
     :score (score/init game window)
     :game-over (gameover/init)
     :paused true}))

(defn update- [{:keys [delta-time state game window]
                {world :world paused :paused { gameover :is-game-over } :game-over} :state}]
  (let [keyboard (g/keyboard-state)]
    (exit-on-esc game keyboard)

    (if paused
      (handle-pause state keyboard)

      (do
        (physics/step world delta-time)
        (-> state
            (update :background background/update- delta-time)
            (update :floor floor/update- delta-time)
            (gameover/update- world delta-time)
            (update :bird bird/update- keyboard gameover delta-time)
            (pipes/update- window world delta-time))))))

(defn draw [{:keys [sprite-batch graphics-device]
             {:keys [floor background pipe-manager bird score] } :state}]
  (g/clear graphics-device :light-gray)
  (g/begin sprite-batch)

  (background/draw sprite-batch background)
  (pipes/draw sprite-batch pipe-manager)
  (floor/draw sprite-batch floor)
  (bird/draw sprite-batch bird)
  (score/draw sprite-batch score)
  (g/end sprite-batch))

;; called from C#
(def Initialize (partial state/Initialize initialize))
(def LoadContent (partial state/LoadContent (constantly nil)))
(def Update (partial state/Update update-))
(def Draw (partial state/Draw draw))
(println "Hi Delboni")
