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
    showtimes
    |> List.map (fun time -> time.ToString())

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
let loadLayoutFromFile filePath =
    if File.Exists(filePath) then
        File.ReadAllLines(filePath)
        |> Array.toList
        |> List.map (fun line ->
            let parts = line.Split(',')
            if parts.Length >= 3 then
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
                (row, column, status)
            else
                failwithf "Malformed line in file: %s" line)
    else
        []
 

 
// Reserve Seat Function with Validations
 
let ReserveSeat (row: int) (column: int) (CustomerName: string) (showtime: DateTime) (layout: SeatLayout) (updateTicketDetails: string -> unit) =
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
            File.AppendAllText("D:\PL3\Ticket.txt", ticketDetails)
            printfn "Seat (%d, %d) reserved for %s with Ticket ID %O" row column customer.Name ticketId
            updateTicketDetails ticketDetails
             
            updatedLayout
        | Some (_, _, Reserved existingCustomer) ->
            printfn "Error: Seat (%d, %d) is already reserved by %s with ID %O." row column existingCustomer.Name existingCustomer.Id
            layout
        | None ->
            printfn "Error: Seat (%d, %d) does not exist." row column
            layout

// Display Seat Layout
let seatLayout =
    try
        let layout = loadLayoutFromFile "D:\PL3\layout.txt"
        if layout.IsEmpty then
            let defaultLayout = InitializeSeat 5 8
            saveLayoutToFile defaultLayout "D:\PL3\layout.txt"
            defaultLayout
        else
            layout
    with
    | ex ->
        printfn "Error loading layout: %s. Using default layout." ex.Message
        let defaultLayout = InitializeSeat 5 8
        saveLayoutToFile defaultLayout "D:\PL3\layout.txt"
        defaultLayout

 //cancle reservation
let cancelReservation (row:int) (column:int) (layout:SeatLayout)=
     layout
     |>List.map (fun (r,c,status)->if r=row && c=column then (r,c,Available) else (r,c,status))
     
let cancelAndSaveReservation row column layout filePath =
    let updatedLayout = cancelReservation row column layout
    saveLayoutToFile updatedLayout filePath
    updatedLayout

