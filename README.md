# Simple database project
A simple database written in c#, using an encrypted TCP channel with file encryption to exchange data between the client and the server's file system.


## How to run
```shell
dotnet run
```
This will run the server, currently with an unconfigurable port of 5000 <br>
Default files will be created, such as the datafile for storing credentials for users/logins.
[Click here](#users) for more information about users.

## How to use
There are two ways to use this database server:
1. Use an ORM to easily get functions like `RegisterSchema`, `<SchemaDefinitionBuilder>.Get`, etc.
2. Send packets manually using the communication protocol

## Commands
Once the server has started up, you can enter commands through the stdin.
Each command has a "help"-subcommand which can be triggered using `<command> help` or `<command> ?`
### Users
Adding users:
```shell
users add <username> <password>
```

Show a list of current users:
```shell
users list
```