module Actor.GameHub.Extensions

open Akka.Actor
open Akka.FSharp

let stoppingStrategy =
    SpawnOption.SupervisorStrategy((new StoppingSupervisorStrategy()).Create())

let monitorWith message (watcher: ICanWatch) subject = watcher.WatchWith(subject, message)
