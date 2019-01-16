# Giraffe.GoodRead [![Build Status](https://travis-ci.org/Zaid-Ajaj/Giraffe.GoodRead.svg?branch=master)](https://travis-ci.org/Zaid-Ajaj/Giraffe.GoodRead) [![Nuget](https://img.shields.io/nuget/v/Giraffe.GoodRead.svg?colorB=green)](https://www.nuget.org/packages/Giraffe.GoodRead)

Practical dependency injection for [Giraffe](https://github.com/giraffe-fsharp/Giraffe) that gets out of your way.

# Install
```bash
# using nuget client
dotnet add package Giraffe.GoodRead
# using Paket
.paket/paket.exe add Giraffe.GoodRead --project path/to/Your.fsproj
```

# Usage

This library consists of a couple of things:
 - `Require.services` function
 - `require` workflow
 - `service<'t>()` function 
 - `Require.apply` function

`Require.service` might be the only one you need from the list, it lets resolve the services registered at asp.net core IoC container:
```fs
GET 
  >=> route "/logger" 
  >=> Require.services<ILogger>(
        fun logger ->
            logger.LogInformation("Using the ILogger") 
            text "Finished using depdendencies"
        )
```
`Require.services` is overloaded and allows to resolve multiple dependencies (for now up to 4):
```fs
GET 
  >=> route "/logger"
  >=> Require.services<ILogger, IConfiguration>(
      fun logger config ->
        // user dependencies and return HttpHandler
        text "Finished using depdendencies") 
```
You can also return `Async<HttpHandler>` or `Task<HttpHandler>` in the function of `Require.services`:
```fs
GET 
  >=> route "/users"
  >=> Require.services<ILogger, IUserStore>(
      fun logger users ->
        logger.LogInformation("Requesting users...")
        async {
            let! allUsers = users.getAll()
            return setStatusCode 200 >=> json allUsers
        }
    ) 
```
Now this is a bit hard to unit-test because you have to test the entire http pipeline with AspNet Core's [Integration tests](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-2.2) but you don't have to if you just want to unit-test a couple of functions. 

To make a function unit-testable, You have to think about two things:
 - What *dependencies* do you need
 - How to map the *output* of the function to `HttpHandler`


An example shows best how these are used
```fs
type User = { Id: int; Username: string }

type IUserStore =
    abstract getAll : unit -> Async<User list>
    abstract findById : int -> Async<Option<User>>

type FindUserResult = 
    | Found of User
    | NotFound 
    | ServerError of exn

let getUserById (userId: int) (logger: ILogger) (userStore: IUserStore) = 
    async {
        let! foundUser = Async.Try (userStore.findById userId)
        match foundUser with 
        | Ok (Some user) -> return Found user
        | Ok None -> return NotFound
        | Error err -> 
            do logger.LogError(err, "Error while searching for user")
            return ServerError err
    }
```
Now you have  and how to map `FindUserResult` to `HttpHandler`. The latter is simple:
```fs
let mapUserResult = function 
    | Found user -> json (Ok user)
    | NotFound -> setStatusCode 404 >=> json (Error "User was not found")
    | ServerError err -> setStatusCode 500 >=> json (Error "Internal server error") 
```
Now you can use `Require.services` again to resolve the dependencies, but you still need the user id from the request, this is done as follows:
```fs
GET 
  >=> routef "/user/%d" (fun userId -> Require.services(getUserById userId, mapUserResult))
```
This works nicely because when your function `getUserById` doesn't return `HttpHandler` directly but returns some `Async<'t>` or `Task<'t>` then you have to provide the second argument which tells Giraffe how to map the `'t` to `HttpHandler`. 

This is how you make your functions unit-testable. Only sometimes you don't really care about unit-testing and you just want to prototype things and write apps in rapid mode! Then allow me to introduce the `require` workflow:

The `require` workflow is a simple implementation of a reader monad that allows you to require registered services, using the `service<'t>()` function, then you can start using the dependencies. Here is an example of using it:
```fs
let getAllUsers() =
    require {
        // require dependencies
        let! logger = service<ILogger>()
        let! userStore = service<IUserStore>()
        // return Async<HttpHandler> or Task<HttpHandler>
        return async {
            let! users = Async.Catch (userStore.getAll())
            match users with
            | Choice1Of2 users -> return json users
            | Choice2Of2 err ->
                logger.LogError(err, "Error while getting all users")
                return setStatusCode 500 >=> json (Error "Internal server error occured")
        }
    }

// apply the requirements and turn getAllUsers into a HttpHandler:
let webApp = route "/users" >=> Require.apply (getAllUsers())
```
if your function happens to have parameters itself, then just provide them from the request:
```fs
let getUserById userId = 
    require {
        let! logger = service<ILogger>()
        let! userStore = service<IUserStore>()
        return async {
            let! user = Async.Try (userStore.getAll())
            match user with 
            | Ok (Some user) -> return json (Ok user)
            | Ok None -> return setStatusCode 404 >=> json (Error "User was not found")
            | Error err -> 
                do logger.LogError(err, "Error while searching for user")
                return setStatusCode 500 >=> json (Error "Internal server error")
        }
    }


let webApp = GET >=> routef "/user/%d" (getUserById >> Require.apply)
```

## Unit Testing
Testing the code with mock dependencies is really easy when using the `require` workflow thanks to `TestServer` from ASP.NET Core:
```fs
testList "Giraffe.GoodRead" [

    testCase "Dependencies are correctly resolved" <| fun () ->
        let webApp = GET >=> route "/users" >=> Require.apply (Users.getAll())
        let logger = Mock.Of<ILogger>()
        let emptyUserList = [ ]
        let emptyUserStore = TestUserStore(emptyUserList)
    
        let setup (services: IServiceCollection) =
            services.AddSingleton<ILogger>(logger)
                    .AddSingleton<IUserStore>(emptyUserStore)
    
        let server = new TestServer(createHost webApp setup)
        let client = server.CreateClient()
    
        client
        |> httpGet "/users"
        |> isStatus HttpStatusCode.OK
        |> readText
        |> fromJson<User list>
        |> fun users -> Expect.isEmpty users "Users list should be empty"
]

```

## Builds

![Build History](https://buildstats.info/travisci/chart/Zaid-Ajaj/Giraffe.GoodRead)


### Building


Make sure the following **requirements** are installed in your system:

* [dotnet SDK](https://www.microsoft.com/net/download/core) 2.0 or higher
* [Mono](http://www.mono-project.com/) if you're on Linux or macOS.

```
> build.cmd // on windows
$ ./build.sh  // on unix
```

### Watch Tests

The `WatchTests` target will use [dotnet-watch](https://github.com/aspnet/Docs/blob/master/aspnetcore/tutorials/dotnet-watch.md) to watch for changes in your lib or tests and re-run your tests on all `TargetFrameworks`

```
./build.sh WatchTests
```
