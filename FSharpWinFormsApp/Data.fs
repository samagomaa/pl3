<<<<<<< HEAD
module Data 
=======
﻿module Data
>>>>>>> ede09112c0b57d52903aca9f169c501bcd205b6b
open MySql.Data.MySqlClient

let connectionString = "Server=localhost;Database=cinema;Uid=root;Pwd=;"

let testconnection () =
    use conn = new MySqlConnection(connectionString)
    conn.Open()
    printfn "connection successful!"

testconnection ()
<<<<<<< HEAD
=======


>>>>>>> ede09112c0b57d52903aca9f169c501bcd205b6b
