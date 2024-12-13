module Data
open MySql.Data.MySqlClient

let connectionString = "Server=localhost;Database=cinema;Uid=root;Pwd=;"

let testconnection () =
    use conn = new MySqlConnection(connectionString)
    conn.Open()
    printfn "connection successful!"

testconnection ()


