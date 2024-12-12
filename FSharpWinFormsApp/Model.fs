module Model
open System
open System.IO

// Customer
type Customer = {
   Id: int
   Name: string
}

// Seat Status as enum
type SeatStatus =
 | Available
 | Reserved of Customer 

// Seat
type Seat = {
    Row: int
    Column: int
    Status: SeatStatus
}

// Ticket
type Ticket = {
    TicketID: Guid
    SeatDetails: Seat
    CustomerDetails: Customer
    Showtime: DateTime
}

// Generate unique Customer ID
let mutable customerIdCounter = 0
let GenerateUniqueCustomerID() =
    customerIdCounter <- customerIdCounter + 1
    customerIdCounter

// Generate unique Ticket ID
let GenerateUniqueTicket() =
    Guid.NewGuid()

// Seat Layout
type SeatLayout = (int * int * SeatStatus) list

// Initialize Seat Layout
let InitializeSeat rows columns : SeatLayout =
    [for row in 1..rows do
        for column in 1..columns do
            (row, column, Available)]

// Define available showtimes
let showtimes = [
    DateTime.Now.AddHours(1.0)
    DateTime.Now.AddHours(2.0)
    DateTime.Now.AddHours(3.0)
]

// Function to display available showtimes
let displayShowtimes () =
    showtimes|> List.map (fun time ->time.ToString("hh:mm tt"))

// Save Seat Layout to File
let saveLayoutToFile (layout: SeatLayout) (filePath: string) =
    let serializedLayout =
        layout
        |> List.map (fun (row, column, status) ->
            let statusStr = 
                match status with
                | Available -> "Available"
                | Reserved customer -> sprintf "Reserved:%d:%s" customer.Id customer.Name
            sprintf "%d,%d,%s" row column statusStr)
        |> String.concat "\n"
    File.WriteAllText(filePath, serializedLayout)

// Load Seat Layout from File
let loadLayoutFromFile (filePath: string) =
    if File.Exists(filePath) then
        File.ReadAllLines(filePath)
        |> Array.toList
        |> List.map (fun line ->
            let parts = line.Split(',')
            let row = int parts.[0]
            let column = int parts.[1]
            let status =
                match parts.[2] with
                | "Available" -> Available
                | _ -> 
                    let customerParts = parts.[2].Split(':')
                    let id = int customerParts.[1]
                    let name = customerParts.[2]
                    Reserved { Id = id; Name = name }
            (row, column, status))
    else
        InitializeSeat 10 10

// Reserve Seat Function with Validations
let ReserveSeat (row: int) (column: int) (CustomerName: string) (showtime: DateTime) (layout: SeatLayout) =
    if row < 1 || row > 10 || column < 1 || column > 10 then
        printfn "Error: Invalid seat selection. Row and Column must be between 1 and 10."
        layout
    else
        let customer = { Id = GenerateUniqueCustomerID(); Name = CustomerName }
        match layout |> List.tryFind (fun (r, c, status) -> r = row && c = column) with
        | Some (_, _, Available) ->
            let updatedLayout = layout |> List.map (fun (r, c, status) ->
                if r = row && c = column then (r, c, Reserved customer)
                else (r, c, status))
            let ticketId = GenerateUniqueTicket()
            let ticket = {
                TicketID = ticketId
                SeatDetails = { Row = row; Column = column; Status = Reserved customer }
                CustomerDetails = customer
                Showtime = showtime
            }
            let ticketDetails = sprintf "Ticket ID: %O\nCustomer: %s\nSeat: (%d, %d)\nShowtime: %O\n\n"
                                        ticket.TicketID ticket.CustomerDetails.Name ticket.SeatDetails.Row ticket.SeatDetails.Column ticket.Showtime
            File.AppendAllText("tickets.txt", ticketDetails)
            printfn "Seat (%d, %d) reserved for %s with Ticket ID %O" row column customer.Name ticketId
            updatedLayout
        | Some (_, _, Reserved existingCustomer) ->
            printfn "Error: Seat (%d, %d) is already reserved by %s with ID %O." row column existingCustomer.Name existingCustomer.Id
            layout
        | None ->
            printfn "Error: Seat (%d, %d) does not exist." row column
            layout

// Display Seat Layout
let displaySeats (layout: SeatLayout) =
    let groupedSeats =
        layout
        |> List.groupBy (fun (row, _, _) -> row)
        |> List.sortBy fst
    for (row, seats) in groupedSeats do
        printfn "Row %d:" row
        seats
        |> List.map (fun (_, column, status) ->
            match status with
            | Available -> sprintf "[%d:A]" column
            | Reserved customer -> sprintf "[%d:R(%s)]" column customer.Name)
        |> String.concat " "
        |> printfn "%s"

// Booking Process with Multiple Reservations
let rec bookSeats (layout: SeatLayout) =
    displaySeats layout

    // Get customer name
    printf "Enter your name: "
    let customerName = Console.ReadLine()

    // Get and validate row number
    let rec getValidRow () =
        printf "Enter row number from 1 to 10: "
        match Int32.TryParse(Console.ReadLine()) with
        | true, n when n >= 1 && n <= 10 -> n
        | _ ->
            printfn "Invalid row number. Please try again."
            getValidRow ()  // Recursive call to re-prompt for row

    let row = getValidRow ()

    // Get and validate column number
    let rec getValidColumn () =
        printf "Enter column number from 1 to 10: "
        match Int32.TryParse(Console.ReadLine()) with
        | true, n when n >= 1 && n <= 10 -> n
        | _ ->
            printfn "Invalid column number. Please try again."
            getValidColumn ()  // Recursive call to re-prompt for column

    let column = getValidColumn ()

    // Show available showtimes and validate selection
    displayShowtimes ()
    let rec getValidShowtime () =
        printf "Select a showtime by number (1-3): "
        match Int32.TryParse(Console.ReadLine()) with
        | true, n when n >= 1 && n <= List.length showtimes -> n - 1
        | _ ->
            printfn "Invalid showtime selection. Please try again."
            getValidShowtime ()  // Recursive call to re-prompt for showtime

    let showtimeChoice = getValidShowtime ()
    let selectedShowtime = showtimes.[showtimeChoice]

    // Reserve the seat
    let updatedLayout = ReserveSeat row column customerName selectedShowtime layout

    // Ask the user if they want to book another seat
    printf "Do you want to book another seat? (yes/no): "
    let response = Console.ReadLine().Trim().ToLower()

    match response with
    | "yes" -> bookSeats updatedLayout  // Continue booking recursively
    | _ -> updatedLayout  // Exit and return the final layout



// Main
let filePath = "seat_layout.txt"
let initialLayout = loadLayoutFromFile filePath
let finalLayout = bookSeats initialLayout
saveLayoutToFile finalLayout filePath

//test reserver
//let layout = InitializeSeat 3 3
//let updatedLayout1 = ReserveSeat 1 1 "Alice" (DateTime.Now) layout
//let updatedLayout2 = ReserveSeat 1 1 "Bob"   (DateTime.Now) updatedLayout1
//let updatedLayout3 = ReserveSeat 2 2 "Charlie"   (DateTime.Now) updatedLayout2
//displaySeats updatedLayout3


