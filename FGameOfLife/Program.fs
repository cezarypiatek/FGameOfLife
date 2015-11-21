open System
open System.Windows.Forms
open System.Drawing
open System.Drawing.Drawing2D

let createEmptyBoard width height=
    Array2D.zeroCreate width height

let createInitalBoard width height=
    let board = createEmptyBoard width height
    let rnd = System.Random(DateTime.Now.Millisecond)
    for i=0 to 8000 do
        let x = rnd.Next(width)
        let y = rnd.Next(height)
        let isAlive = rnd.Next(100)%2
        Array2D.set board x y isAlive
    board

let getCellState (board: int[,]) (x, y)=
    let maxX = (Array2D.length1 board)
    let maxY = (Array2D.length2 board)
    if x = -1 || x = maxX || y = -1 || y = maxY then
        0
    else board.[x,y]
    
let getNeighbourhoodCount (x,y) (board: int[,])=
    let n = getCellState board
    n(x, y-1) + n(x-1, y-1) + n(x-1, y) + n(x-1, y+1) + n(x, y+1) + n(x+1, y+1) + n(x+1, y)+ n(x+1, y-1)

let shouldBeAlive isAlive  neightbourCount=
    match isAlive, neightbourCount with
    | 0, 3 -> 1
    | 1,  x when x = 2 || x = 3 -> 1
    | _,_ ->0

let forEachOnBoard board func=
      for x = 0 to (Array2D.length1 board)-1 do
        for y = 0 to (Array2D.length2 board)-1 do
            func x y

let generateNewGeneration board=
    let boardWidth = Array2D.length1 board
    let boardHeight = Array2D.length2 board
    let newBoard = createEmptyBoard boardWidth boardHeight
    forEachOnBoard board (fun x y ->
            let aliveNeightbours = getNeighbourhoodCount (x, y) board
            let isAlive = shouldBeAlive board.[x,y]  aliveNeightbours
            Array2D.set newBoard x y isAlive
    )
    newBoard

let draw board drawSquare=
    forEachOnBoard board (fun x y ->
        if board.[x,y] = 1 then
            drawSquare x y
        )

let createDrawFunc (width,height) (canvas:PictureBox) =
     let g = Graphics.FromImage(canvas.Image)
     let squareSize = canvas.Image.Width /width
     let brush = new LinearGradientBrush(Point(0,0), Point(0,squareSize), Color.Yellow, Color.Orange)
     let drawSquare x y =
        g.FillRectangle(brush, x*squareSize, y*squareSize, squareSize, squareSize)
     fun board -> 
        g.Clear(Color.Green)
        draw board drawSquare
        canvas.Invalidate() 

let createUI()=
    let form = new Form()
    let canvas = new PictureBox()
    let bmp = new Bitmap(1500, 1500)
    canvas.Image <- bmp
    canvas.SizeMode <- PictureBoxSizeMode.Zoom
    canvas.Dock <- DockStyle.Fill
    form.Controls.Add(canvas)
    form.Show()
    (form, canvas)

let form, canvas = createUI()
let boardWidth, boardHeight = 150, 150
let initialBoard = createInitalBoard boardWidth boardHeight
let drawBoardFunc = createDrawFunc (boardWidth, boardHeight) canvas

let loop initialBoard=
    let uiContext = System.Threading.SynchronizationContext.Current
    let rec innerLoop board generationNum =
        async {
            let newGenerationNum = generationNum + 1
            do! Async.SwitchToContext uiContext
            form.Text <- sprintf "Generation: %d" newGenerationNum
            drawBoardFunc board
            let newboard = generateNewGeneration board
            do! Async.Sleep 50
            do innerLoop newboard newGenerationNum
        } |> Async.Start
    innerLoop initialBoard 0
    ()
form.Shown.Add(fun _-> loop(initialBoard))

[<STAThread>]
do Application.Run(form)