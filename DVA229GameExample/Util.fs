module Util

open System.Drawing

// This module contains utility types and functions used in Game.

type Vector = Vector of float * float
    with
        static member (+) (Vector(x1, y1), Vector(x2, y2)) =
            Vector(x1 + x2, y1 + y2)

        static member (*) (Vector(x, y), scale: float) =
            Vector(x * scale, y * scale)
       
module Vector =
    let gravity = Vector(0.0, 9.82) * (1.0 / float Shared.tickPerSecond)

module Brush =
    let cyan = new SolidBrush(Color.Cyan)
    let magenta = new SolidBrush(Color.Magenta)
    let yellow = new SolidBrush(Color.Yellow)

    let cmyBrushes = [|cyan; magenta; yellow|]

    let black = new SolidBrush(Color.Black)
    let white = new SolidBrush(Color.White)

module Pen = 
    let white = new Pen(new SolidBrush(Color.White))

module Rand =
    let rng = System.Random()

    let randInt lo hi = rng.Next(lo, hi)

    let randFloat lo hi = rng.NextDouble() * (hi - lo) + lo

    let randVector (xLo, xHi) (yLo, yHi) =
        Vector(randFloat xLo xHi, randFloat yLo yHi)

    let randCmyBrush () = 
        let n = Array.length Brush.cmyBrushes
        let i = randInt 0 n
        Brush.cmyBrushes.[i]
 
type Timer =
    | Done
    | TicksLeft of int

module Timer =
    let init n = TicksLeft n

    let isDone =
        function
        | Done -> true
        | _ -> false

    let tick =
        function
        | Done -> Done
        | TicksLeft n when n <= 1 -> Done
        | TicksLeft n -> TicksLeft (n - 1)
