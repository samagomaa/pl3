module Model
open System
open System.IO
open Data
open MySql.Data.MySqlClient
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
//save customer in database
let GetOrCreateCustomerId (customerName: string) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()
    use transaction = conn.BeginTransaction()

    try
        // Check if the customer already exists
        let selectQuery = "SELECT Id FROM Customer WHERE Name = @Name FOR UPDATE"
        use selectCmd = new MySqlCommand(selectQuery, conn, transaction)
        selectCmd.Parameters.AddWithValue("@Name", customerName) |> ignore
        
        use reader = selectCmd.ExecuteReader()
        if reader.Read() then
            let existingId = reader.GetInt32(0)
            reader.Close()
            transaction.Commit()
            existingId // Return the existing customer's ID
        else
            reader.Close() // Close the reader before executing the next query
            
            // Insert a new customer and get the auto-incremented ID
            let insertQuery = "INSERT INTO Customer (Name) VALUES (@Name); SELECT LAST_INSERT_ID();"
            use insertCmd = new MySqlCommand(insertQuery, conn, transaction)
            insertCmd.Parameters.AddWithValue("@Name", customerName) |> ignore
            let newId = Convert.ToInt32(insertCmd.ExecuteScalar()) // Convert UInt64 to Int32
            transaction.Commit()
            newId // Return the newly created customer's ID
    with
    | ex ->
        transaction.Rollback()
        raise ex // Re-throw the exception for handling



//sve ticket in database
let SaveTicket (ticket: Ticket) =
    use conn = new MySql.Data.MySqlClient.MySqlConnection(connectionString)
    conn.Open()
    let query = """
        INSERT INTO Ticket (TicketID, CustomerId, SeatRow, SeatColumn, Showtime)
        VALUES (@TicketID, @CustomerId, @SeatRow, @SeatColumn, @Showtime)
    """
    use cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn)
    cmd.Parameters.AddWithValue("@TicketID", ticket.TicketID.ToString()) |> ignore
    cmd.Parameters.AddWithValue("@CustomerId", ticket.CustomerDetails.Id) |> ignore
    cmd.Parameters.AddWithValue("@SeatRow", ticket.SeatDetails.Row) |> ignore
    cmd.Parameters.AddWithValue("@SeatColumn", ticket.SeatDetails.Column) |> ignore
    cmd.Parameters.AddWithValue("@Showtime", ticket.Showtime) |> ignore
    cmd.ExecuteNonQuery()

//save seat
let SaveSeat (ticketId: Guid) (row: int) (col: int) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    let query = "INSERT INTO Seat (Row, Col, TicketID) VALUES (@Row, @Col, @TicketID)"
    use cmd = new MySqlCommand(query, conn)
    cmd.Parameters.AddWithValue("@Row", row) |> ignore
    cmd.Parameters.AddWithValue("@Col", col) |> ignore
    cmd.Parameters.AddWithValue("@TicketID", ticketId.ToString()) |> ignore

    cmd.ExecuteNonQuery()
 
// Reserve Seat Function with Validations
 
let ReserveSeat (row: int) (column: int) (CustomerName: string) (showtime: DateTime) (layout: SeatLayout) (updateTicketDetails: string -> unit) =
    if row < 1 || row > 10 || column < 1 || column > 10 then
        printfn "Error: Invalid seat selection. Row and Column must be between 1 and 10."
        layout
    else
        let customerId = GetOrCreateCustomerId CustomerName
        let customer = { Id = customerId; Name = CustomerName }
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
            SaveTicket ticket
            SaveSeat ticketId row column
            let ticketDetails = sprintf "Ticket ID: %O\nCustomer: %s\nSeat: (%d, %d)\nShowtime: %O\n\n"
                                        ticket.TicketID ticket.CustomerDetails.Name ticket.SeatDetails.Row ticket.SeatDetails.Column ticket.Showtime
            File.AppendAllText("D:\\PL3\\Ticket.txt", ticketDetails)
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
// Function to cancel the reservation and delete the customer from the database and the file
let CancelReservationAndDeleteCustomer (row: int) (col: int) (customerName: string) (layout: SeatLayout) (filePath: string) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    // Delete the ticket associated with the specified seat and customer
    let deleteTicketQuery = """
       DELETE Ticket
            FROM Ticket
            JOIN Seat ON Ticket.TicketID = Seat.TicketID
            JOIN Customer ON Ticket.CustomerId = Customer.Id
            WHERE Seat.Row = @Row AND Seat.Col = @Col AND Customer.Name = @CustomerName
        """
    use cmd = new MySqlCommand(deleteTicketQuery, conn)
    cmd.Parameters.AddWithValue("@Row", row) |> ignore
    cmd.Parameters.AddWithValue("@Col", col) |> ignore
    cmd.Parameters.AddWithValue("@CustomerName", customerName) |> ignore

    let ticketRowsAffected = cmd.ExecuteNonQuery()

    // If the ticket was deleted, delete the customer
    if ticketRowsAffected > 0 then
        let deleteCustomerQuery = """
            DELETE FROM Customer
            WHERE Name = @CustomerName
        """
        use deleteCustomerCmd = new MySqlCommand(deleteCustomerQuery, conn)
        deleteCustomerCmd.Parameters.AddWithValue("@CustomerName", customerName) |> ignore

        let customerRowsAffected = deleteCustomerCmd.ExecuteNonQuery()
        
        if customerRowsAffected > 0 then
            printfn "Customer %s and their reservation have been deleted." customerName
        else
            printfn "Customer %s not found." customerName
    else
        printfn "No reservation found for seat (%d, %d) by %s." row col customerName

    // Update seat layout in memory (set seat back to Available)
    let updatedLayout = layout |> List.map (fun (r, c, status) ->
        if r = row && c = col then (r, c, Available)
        else (r, c, status))

    // Save updated layout to file
    saveLayoutToFile updatedLayout filePath
    updatedLayout
<<<<<<< HEAD
=======

 
       
     
 

>>>>>>> ede09112c0b57d52903aca9f169c501bcd205b6b
