(ns cljgame.game
    (:require [cljgame.monogame :as g]
              [cljgame.state :as state]
              [cljgame.entities.logo :as logo]
              [cljgame.entities.ball :as ball]
              [cljgame.entities.player :as player]
              [cljgame.entities.score :as score])
  (:import [System Console]))

(defn exit-on-esc [game]
  (when (-> (g/keyboard-state) (g/is-key-dowm :escape))
        (g/exit game)))

(defn init [game { graphics :graphics-manager
                   window :window }]

  (g/set-mouse-visible game true)
  (g/set-screen-size graphics {:width 1024 :height 768})
  (g/apply-changes graphics)

  {:logo (logo/init window)
   :player1 (player/init window :player1 game)
   :player2 (player/init window :player2 game)
   :ball (ball/init window game)
   :score (score/init game) })

(defn load-content [game {state :state}]
  (-> state
      (update :logo logo/load- game)))

(defn update- [{:keys [delta-time state game window] }]
  (exit-on-esc game)
  (-> state
      (update :logo logo/update- delta-time)
      (update :ball ball/update- delta-time window (select-keys state [:player1 :player2]))
      (update :player1 player/update- delta-time window)
      (update :player2 player/update- delta-time window)
      (score/update- window)))


(defn draw [{:keys [sprite-batch graphics-device window]
             { :keys [player1 player2 logo ball score] } :state}]
  (g/clear graphics-device :light-gray)
  (g/begin sprite-batch)

  (logo/draw sprite-batch logo)
  (player/draw sprite-batch player1)
  (player/draw sprite-batch player2)
  (score/draw sprite-batch window score)
  (ball/draw sprite-batch ball)

  (g/end sprite-batch))

;; called from C#
(def Initialize (partial state/Initialize init))
(def LoadContent (partial state/LoadContent load-content))
(def Update (partial state/Update update-))
(def Draw (partial state/Draw draw))
(println "Hi Delboni")
