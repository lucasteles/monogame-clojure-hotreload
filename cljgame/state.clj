(ns cljgame.state)

(def props (atom {:state {}}))

(defn Initialize [initialize-fn game sprite-batch graphics-manager graphics-device window]
  (swap! props merge
      {:sprite-batch sprite-batch
       :graphics-manager graphics-manager
       :graphics-device graphics-device
       :window window})
  (let [state (initialize-fn game @props)]
    (when state (swap! props assoc :state state))))

(defn LoadContent [load-fn game]
  (let [p @props
        state (load-fn game @props)]
    (when state (swap! props assoc :state state))))

(defn Update [update-fn game game-time]
  (let [props' @props
        state (:state props')
        new-state (update-fn {:game game
                              :game-time game-time
                              :delta-time (-> game-time .ElapsedGameTime .TotalSeconds)
                              :state state
                              :window (:window props')
                              :graphics-manager (:graphics-manager props')})]
    (when (not (identical? state new-state))
          (swap! props assoc :state new-state))))

(defn Draw [draw-fn game game-time]
  (let [props' @props]
    (draw-fn {:game game
              :delta-time (-> game-time .ElapsedGameTime .TotalSeconds)
              :game-time game-time
              :state (:state props')
              :sprite-batch (:sprite-batch props')
              :graphics-device (:graphics-device props')
              :window (:window props')})))
