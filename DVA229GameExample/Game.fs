module Game

open Util
open System.Drawing
open FSharpx.Control.Observable
open Gui

// IGui lets us communicate with the GUI without knowing anything about its
// implementation. This technique is called dependency injection, and it is very 
// useful. By defining our logic in terms of this interface, we make sure that the
// simulation does not depend on certain values in certain GUI components, 
// or even worse, makes use of global mutable state.
type IGui =
    abstract member GetGraphics: unit -> Graphics

    abstract member UpdateBallCounter: int -> unit

    abstract member ResetSpawnCounter: unit -> unit

    abstract member UpdateSpawnCounter: int -> unit

type Ball = 
    { x: float
    ; y: float
    ; r: float
    ; vel: Vector
    ; brush: Brush
    }

module Ball =
    let init x y =
        // Note that -y is up in windows forms.
        let minVelX = -250.0 / float Shared.tickPerSecond
        let maxVelX =  250.0 / float Shared.tickPerSecond
        let maxVelY = -250.0 / float Shared.tickPerSecond
        let minVelY = -750.0 / float Shared.tickPerSecond

        { x = x
        ; y = y
        ; r = Rand.randFloat 10.0 25.0
        ; vel = Rand.randVector (minVelX, maxVelX) (minVelY, maxVelY)
        ; brush = Rand.randCmyBrush ()
        }

    let tick ({ x = x; y = y; r = r; vel = vel } as ball) =
        // Apply gravity to velocity.
        let Vector(vx, vy) as vel = vel + Vector.gravity 
        // Compute new ball.
        { ball with x = x + vx; y = y + vy; vel = vel }

    let shouldLive { x = x; y = y; r = r } =
        // If y + r < 0.0 we let the balls live. This makes the
        // simulation seem more realistic, because we allow the balls
        // to come back after flying out through the top of the game area.
        0.0 <= x + r && x - r < float Shared.gameAreaWidth &&
        y - r < float Shared.gameAreaHeight

    let draw (g: Graphics) { x = x; y = y; r = r; brush = brush } =  
        Gui.graphics.FillEllipse(brush, int x , int y, int r, int r)

module Graphics =
    // Points representing an X, translated by (x, y).
    let makeX (x, y) scale = 
       [| Point(x - scale * 5, y + scale * 3)
        ; Point(x - scale * 3, y + scale * 5)
        ; Point(x, y + scale * 2)

        ; Point(x + scale * 3, y + scale * 5)
        ; Point(x + scale * 5, y + scale * 3)
        ; Point(x + scale * 2, y) 

        ; Point(x + scale * 5, y - scale * 3)
        ; Point(x + scale * 3, y - scale * 5)
        ; Point(x, y - scale * 2) 

        ; Point(x - scale * 3, y - scale * 5)
        ; Point(x - scale * 5, y - scale * 3)
        ; Point(x - scale * 2, y) 
        |]

    let drawX xy pen (g: Graphics) = 
        g.DrawPolygon(pen, makeX xy 2)

    let fillX xy brush (g: Graphics) = 
        g.FillPolygon(brush, makeX xy 2)


type GameState = 
    { balls: Ball []
    ; spawnPoint: (int * int) option
    ; spawnTimer: Timer option
    ; spawnCounter: int option
    }

module GameState =
    let init = 
        { balls = [||]
        ; spawnPoint = None
        ; spawnTimer = None
        ; spawnCounter = None 
        }
   
type MousePosition = int * int

type Event =    
    | Tick
    | AutoSpawn of int
    | MouseLeftClick of MousePosition

let rec loop (gui: IGui) eventStream gameState = async {
    // Waiting on and then matching on the next event.
    match! Async.AwaitObservable eventStream with
    | AutoSpawn n -> 
        let timer = Timer.init Shared.ballSpawnRate
        
        let gameState =
            { gameState with spawnCounter = Some n 
                           ; spawnTimer = Some timer }

        return! loop gui eventStream gameState

    | MouseLeftClick mousePos ->
        return! loop gui eventStream { gameState with spawnPoint = Some mousePos } 

    | Tick -> 
        // Update spawnCounter and spawnTimer. Spawn a ball if we should.
        let spawnCounter, spawnTimer, newBall =
            match gameState.spawnCounter, gameState.spawnTimer with
            | None  , _            -> None, None, None
            | Some n, _ when n < 1 -> 
                gui.ResetSpawnCounter()
                None, None, None

            // We only spawn a new ball if the timer is done and 
            // if there exists a spawn point in the game area.
            | Some n, Some(timer) when Timer.isDone timer ->
                gameState.spawnPoint
                // Translating the ball by -10, -15 makes it look a bit better.
                |> Option.map (fun (x, y) -> float (x - 10), float (y - 15))
                |> Option.map (fun (x, y) -> Ball.init x y)
                |> fun ball -> 
                    let timer = Timer.init Shared.ballSpawnRate
                    Some (n - 1), Some timer, ball

            | Some n, timer -> Some n, Option.map Timer.tick timer, None

        // Update the spawn counter in the GUI if we should.
        Option.iter (fun n -> gui.UpdateSpawnCounter n) spawnCounter

        // Update balls.
        let balls = 
            Array.map Ball.tick gameState.balls
            |> Array.filter Ball.shouldLive 

        gui.UpdateBallCounter (Array.length balls)

        // Add the new ball (if there is one).
        let balls = 
            newBall
            |> Option.map Array.singleton
            |> Option.defaultValue [||]
            |> Array.append balls

        // Clear game area.
        Gui.graphics.FillRectangle(Brush.black, 0, 0, Shared.gameAreaWidth, Shared.gameAreaWidth)

        // Draw spawnpoint.
        Option.iter (fun (x, y) ->
            Graphics.fillX (x, y) Brush.white Gui.graphics
        ) gameState.spawnPoint

        // Draw balls.
        Array.iter (Ball.draw Gui.graphics) balls

        // Build the new game state.
        let gameState = 
            { gameState with balls = balls
                           ; spawnCounter = spawnCounter
                           ; spawnTimer = spawnTimer }

        // Go to the next iteration.
        return! loop gui eventStream gameState
}
