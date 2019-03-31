module Gui

open System.Drawing
open System.Windows.Forms

// We need this to avoid the flickering.
let doubleBuffered = 
    ControlStyles.AllPaintingInWmPaint |||
    ControlStyles.UserPaint |||
    ControlStyles.DoubleBuffer

// We inherit from Panel for two reasons:
//   * It is the simplest way (that I know of) to enable doule buffering.
//   * We can define custom events for when the user clicks different buttons.
type GameArea () as this =
    inherit Panel()

    let mouseLeftClick = Event<int * int>()
    let mouseRightClick = Event<int * int>()

    do 
        // Disable flickering.
        this.SetStyle(doubleBuffered, true)

        // Logic for our custom events.
        this.MouseClick.Add(fun e -> 
            match e.Button with
            | MouseButtons.Left  -> mouseLeftClick.Trigger(e.X, e.Y)
            | MouseButtons.Right -> mouseRightClick.Trigger(e.X, e.Y)
            | _  -> ()
        )

    // Publishing makes the events available for use with out obserrvables.
    member this.MouseLeftClick = mouseLeftClick.Publish
    member this.MouseRightClick = mouseRightClick.Publish

// The simplest way (that I could find) to add a colored border.
// Apparently, this method does not follow best practices - it does
// not work well with scrollbars! But who wants scrollbars in a game?
let addBorder color (control : Control) =
    control.Paint.Add(fun (e : PaintEventArgs) ->
        ControlPaint.DrawBorder(
            e.Graphics, 
            control.ClientRectangle, 
            color,
            ButtonBorderStyle.Solid
        );
    )

// Colors
let textColor = Color.White
let borderColor = Color.White
let backgroundColor = Color.Black

// Dimensions of the drawing surface.
let gameAreaWidth = Shared.gameAreaWidth
let gameAreaHeight = Shared.gameAreaHeight

// Height of the area designated for the UI components.
let guiPanelWidth = gameAreaWidth
let guiPanelHeight = 50

// The font used for all texts.
let font = new Font("Monospace", 24.0f)

 // The form representing the main window.
let window = 
    new Form(
        Text = "Balls!",
        TopMost = true,
        // + 16 makes the right border of the surface visible.
        Width = gameAreaWidth + 16,
        // + 38 makes the bottom border of the surface visible.
        Height = guiPanelHeight + gameAreaHeight + 38,
        BackColor = backgroundColor,
        MinimizeBox = false, // Removes the minimize button.
        MaximizeBox = false, // Removes the maximize button.
        // Disables dragging-the-border resizing.
        FormBorderStyle = FormBorderStyle.FixedSingle
    )

let guiPanel = 
    let panel = 
         new Panel(
             Width = gameAreaWidth,
             Height = guiPanelHeight,
             BackColor = backgroundColor
         )

    addBorder borderColor panel

    panel

// Text label showing the numbers of balls in the game.
let balls =
    let label = 
        new Label(
            Location = Point(0, gameAreaHeight - 1),
            Text = "balls:",
            Width = 190,
            Height = guiPanelHeight,
            ForeColor = textColor,
            BackColor = backgroundColor,
            Font = font,
            TextAlign = ContentAlignment.MiddleLeft
        )

    addBorder borderColor label

    label

let numBalls = 
    let textBox = 
        new TextBox(
            Location = Point(balls.Width - 110, balls.Location.Y + 6),
            Text = "12345",
            // The only way to set the size of a TextBox.
            MaximumSize = Size(100, guiPanelHeight - 2),
            ReadOnly = true,
            // Only allow 7 digits.
            ForeColor = textColor,
            BackColor = backgroundColor,
            Font = font,
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Right
        )

    textBox

let howTo =
    let label = 
        new Label(
            Location = Point(0, 0),
            Text = "Click below to set a spawn point! Then ->",
            Width = 615,
            Height = guiPanelHeight,
            ForeColor = textColor,
            BackColor = backgroundColor,
            Font = font,
            TextAlign = ContentAlignment.MiddleCenter
        )

    addBorder borderColor label

    label

let go =
    let label = 
        new Button(
            Location = Point(howTo.Width, 0),
            Text = "Go!",
            Width = 75,
            Height = guiPanelHeight,
            ForeColor = textColor,
            BackColor = backgroundColor,
            Font = font,
            TextAlign = ContentAlignment.MiddleCenter
        )

    addBorder borderColor label

    label

let spawnCounter = 
    let textBox = 
        new TextBox(
            Location = Point(howTo.Width + go.Width, 1),
            Text = "17",
            // The only way to set the size of a TextBox.
            MinimumSize = Size(110, guiPanelHeight),
            // Only allow 7 digits.
            MaxLength = 5,
            ForeColor = textColor,
            BackColor = backgroundColor,
            Font = font,
            TextAlign = HorizontalAlignment.Right
        )

    addBorder borderColor textBox

    textBox

// This is the surface where the drawing happens.
let gameArea = 
    let panel = 
        new GameArea(
            BackColor = Color.Black,
            // - 1 avoids double border between guiPanel and surface.
            Location = Point(0, guiPanelHeight - 1),
            Width = gameAreaWidth,
            Height = gameAreaHeight
        )

    panel

// Add GUI components to GUI panel.
guiPanel.Controls.Add howTo
guiPanel.Controls.Add go
guiPanel.Controls.Add spawnCounter

// Add GUI components and the surface to the window.
window.Controls.Add guiPanel
window.Controls.Add numBalls 
window.Controls.Add balls 
window.Controls.Add gameArea 

// We need this to be able to draw on the surface!
let graphics = gameArea.CreateGraphics() 
