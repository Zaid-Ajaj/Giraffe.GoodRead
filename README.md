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

This library consists of three things: 
 - `require` workflow
 - `service<'t>()` function 
 - `Require.apply` function

An example shows best how these are used
```fs
type User = { Id: int; Username: string }

type IUserStore =
    abstract getAll : unit -> Async<User list>
    abstract getById : int -> Async<Option<User>>

let getUserById userId =
    require {
        let! logger = service<ILogger>()
        let! userStore = service<IUserStore>()
        return async {
            match! Async.Try (userStore.getById userId) with
            | Ok (Some user) ->
                return json (Ok user)
            | Ok None ->
                return setStatusCode 404 >=> json (Error "User was not found")
            | Error ex ->
                do logger.LogError(ex, "Error while searching user")
                return setStatusCode 500 >=> json (Error "Internal server error occured")
        }
    }


let webApp = GET >=> routef "/user/%d" (getUserById >> Require.apply)

```
The `require` workflow is a simple implementation of a reader monad that allows you to require registered services, using the `service<'t>()` function, then you can start using the dependencies. The workflow `getUserById` can be used as input for `Require.apply` as long as it returns any of:
 - `HttpHandler`
 - `Async<HttpHandler>` (e.g. the example above)
 - `Task<HttpHandler>` 

Then `Require.apply` turns the workflow itself into a `HttpHandler` which makes it really easy and simple to build hanlders with dependency injection. 

## Unit Testing
Testing the code with mock dependencies is really easy thanks to `TestServer` that ASP.NET provides:
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
