namespace Giraffe.GoodRead

type Reader<'a, 'b> = Reader of ('a -> 'b)

module Reader =
    let run input (Reader fromAtoB) =
        let output = fromAtoB input
        output

    let returnM b = Reader (fun _ -> b)

    let bindM f reader =
        let nextReader environment =
            let x = run environment reader
            run environment (f x)
        Reader nextReader

type ReaderBuilder<'t, 'u>() =
    member this.Return(x) = Reader.returnM x
    member this.Bind(x, f) = Reader.bindM f x
