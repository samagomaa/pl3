open System
open System.Drawing
open System.Windows.Forms

// Create the main form
let mainForm = new Form(Text = "Cinema Reservation Seat", Width = 900, Height = 600)
mainForm.BackColor <- Color.FromArgb(255, 255, 225)
mainForm.StartPosition <- FormStartPosition.CenterScreen

// Create a label for the title
let titleLabel = new Label(
    Text = "Cinema Reservation Seat", 
    AutoSize = true, 
    Location = new Point(350, 10), 
    Font = new Font("Arial", 16.0f, FontStyle.Bold),
    ForeColor = Color.FromArgb(255, 127, 80)
)
mainForm.Controls.Add(titleLabel)

// Grid settings
let rows = 5
let columns = 8
let buttonSize = 50

// Create a Label and TextBox for Column
let columnLabel = new Label(Text = "Enter Column", Location = new Point(50, 390))
let columnInput = new TextBox(Location = new Point(150, 390), Width = 200)
mainForm.Controls.Add(columnLabel)
mainForm.Controls.Add(columnInput)

// Create a Label and TextBox for Row
let rowLabel = new Label(Text = "Enter Row", Location = new Point(50, 430))
let rowInput = new TextBox(Location = new Point(150, 430), Width = 200)
mainForm.Controls.Add(rowLabel)
mainForm.Controls.Add(rowInput)


// Create a 2D array of seat buttons
let seats = Array2D.init rows columns (fun i j ->
    let button = new Button(Text = sprintf "%d,%d" (i + 1) (j + 1), 
                            Size = new Size(buttonSize, buttonSize), 
                            Location = new Point(50 + j * (buttonSize + 5), 50 + i * (buttonSize + 5)))
    button.BackColor <- Color.FromArgb(255, 215, 0)
    button.Click.Add(fun _ ->
        rowInput.Text <- (i + 1).ToString()
        columnInput.Text <- (j + 1).ToString()
    )
    mainForm.Controls.Add(button)
    button)

// Create a Label and TextBox for Username
let userNameLabel = new Label(Text = "Enter UserName", Location = new Point(50, 350))
let userNameInput = new TextBox(Location = new Point(150, 350), Width = 200)
mainForm.Controls.Add(userNameLabel)
mainForm.Controls.Add(userNameInput)


// Create a ComboBox for Show Time
let showTimeLabel = new Label(Text = "Select Show Time", Location = new Point(50, 470))
let showTimeComboBox = new ComboBox(Location = new Point(150, 470), Width = 200)
showTimeComboBox.Items.Add("10:00 AM")
showTimeComboBox.Items.Add("2:00 PM")
showTimeComboBox.Items.Add("6:00 PM")
showTimeComboBox.SelectedIndex <- 0
mainForm.Controls.Add(showTimeLabel)
mainForm.Controls.Add(showTimeComboBox)

// Create a label to display ticket details
let ticketDetailsLabel = new Label(
    Text = "Display Ticket Details", 
    AutoSize = false,
    Location = new Point(550, 350), 
    Size = new Size(300, 200),
    Font = new Font("Arial", 10.0f, FontStyle.Bold),
    BorderStyle = BorderStyle.FixedSingle,
    ForeColor = Color.FromArgb(255, 127, 80),
    TextAlign = ContentAlignment.MiddleCenter
)
mainForm.Controls.Add(ticketDetailsLabel)

// Function for updating ticket details
let updateTicketDetails (userName: string) (row: int) (col: int) (showTime: string) =
    ticketDetailsLabel.Text <- 
        sprintf "Ticket Reserved for %s\nRow: %d, Column: %d\nShow Time: %s" userName row col showTime

// Function for "Reserve" button
let reserveButton_Click (sender: obj) (e: EventArgs) =
    let userName = userNameInput.Text
    let row = Int32.TryParse(rowInput.Text)
    let column = Int32.TryParse(columnInput.Text)
    let showTime = showTimeComboBox.SelectedItem.ToString()
    match row, column with
    | (true, r), (true, c) when r > 0 && r <= rows && c > 0 && c <= columns ->
        let seatButton = seats.[r - 1, c - 1]
        if not seatButton.Enabled then
            MessageBox.Show("Seat already reserved!") |> ignore
        else
            seatButton.BackColor <- Color.FromArgb(255, 140, 0)
            seatButton.Enabled <- false
            updateTicketDetails userName r c showTime
    | _ ->
        MessageBox.Show("Invalid row or column!") |> ignore

// Create buttons for actions
let reserveButton = new Button(Text = "Reserve", Location = new Point(50, 510))
reserveButton.BackColor <- Color.FromArgb(255, 165, 0)
reserveButton.ForeColor <- Color.White
reserveButton.Click.Add(fun e -> reserveButton_Click reserveButton e)

let clearButton = new Button(Text = "Clear", Location = new Point(150, 510))
clearButton.BackColor <- Color.FromArgb(255, 165, 0) 
clearButton.ForeColor <- Color.White
clearButton.Click.Add(fun _ ->
    userNameInput.Clear()
    columnInput.Clear()
    rowInput.Clear()
    showTimeComboBox.SelectedIndex <- 0
    ticketDetailsLabel.Text <- "Display Ticket Details"
)

let bookAnotherSeatButton = new Button(Text = "Book Another Seat", Location = new Point(250, 510))
bookAnotherSeatButton.BackColor <- Color.FromArgb(255, 165, 0) 
bookAnotherSeatButton.ForeColor <- Color.White
bookAnotherSeatButton.Click.Add(fun _ ->
    MessageBox.Show("You can book another seat now.") |> ignore
)

let finishBookingButton = new Button(Text = "Finish Booking", Location = new Point(400, 510))
finishBookingButton.BackColor <- Color.FromArgb(255, 165, 0) 
finishBookingButton.ForeColor <- Color.White
finishBookingButton.Click.Add(fun _ ->
    MessageBox.Show("Thank you for booking your ticket!", "Booking Finished", MessageBoxButtons.OK) |> ignore
    Application.Exit()
)

mainForm.Controls.Add(reserveButton)
mainForm.Controls.Add(clearButton)
mainForm.Controls.Add(bookAnotherSeatButton)
mainForm.Controls.Add(finishBookingButton)

// Run the application
[<EntryPoint>]
let main argv =
    Application.Run(mainForm)
    0
