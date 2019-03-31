module Program

// The timer used to generate ticks.
let timer = new System.Timers.Timer(1000.0 / float Shared.tickPerSecond)

// Active pattern for parsing integers.
let (|Int|_|) (s: string) =
    if not (Seq.isEmpty s) && Seq.forall System.Char.IsDigit s
        then Some(int s)
        else None

// We merge the outputs of all observables into an event stream. 
let eventStream = 
    let observables = 
        [ Observable.map (fun _ -> Game.Tick) timer.Elapsed

        ; Gui.gameArea.MouseLeftClick
          |> Observable.map (fun (x, y) -> Game.MouseLeftClick(x, y))

        ; Gui.go.Click
          |> Observable.map (fun _ ->
                match Gui.spawnCounter.Text with
                // Here we use the active pattern.
                | Int n when 0 < n -> 
                    Gui.spawnCounter.Enabled <- false
                    Some(Game.AutoSpawn n)

                | _ -> None
             )
            // We choose based on the identity function, id or (fun x -> x).
            // This results in the stream ignoring events with the value None,
            // and extracting the x out of values that are Some(x).
          |> Observable.choose id
        ]

    Seq.reduce Observable.merge observables

// Here we implement the IGui interface from Game.
type Gui() =
    interface Game.IGui with 
        member this.GetGraphics () = Gui.graphics

        member this.ResetSpawnCounter () =
            Gui.spawnCounter.Text <- ""
            Gui.spawnCounter.Enabled <- true

        member this.UpdateSpawnCounter n =
            Gui.spawnCounter.Text <- string n

        member this.UpdateBallCounter n =
            Gui.numBalls.Text <- string n

[<EntryPoint>]
let main _ = 
    timer.Start()
    Async.StartImmediate(Game.loop (Gui ()) eventStream Game.GameState.init)
    System.Windows.Forms.Application.Run(Gui.window)
    0
