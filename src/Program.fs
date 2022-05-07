open System.Numerics
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open System.Linq
open Bogus

let fake = Faker()

[<PlainExporter; MemoryDiagnoser>]
type Benchmark () =
  let mutable xs = Array.empty
  
  [<GlobalSetup>]
  member __.Setup() =
    xs <- [| for _ in 1..10000 do fake.Random.Short() |> int |]

  [<Benchmark>]
  member __.Vector_add() = 
    let mutable subtotal = Vector<int>()
    let simdlen = Vector<int>.Count
    let lastindex = xs.Length - (xs.Length % simdlen)

    for i in 0..simdlen..(lastindex - 1) do
      subtotal <- subtotal + Vector<int>(xs, i)

    let mutable total = 0
    for i = 0 to simdlen - 1 do
      total <- total + subtotal[i]

    for i = lastindex to xs.Length - 1 do
      total <- total + xs[i]

    total

  [<Benchmark>]
  member __.Vector_add_as_int64() = 
    let mutable subtotal = Vector<int64>()
    let simdlen = Vector<int>.Count
    let lastindex = xs.Length - (xs.Length % simdlen)
    let mutable l1 = Vector<int64>()
    let mutable l2 = Vector<int64>()

    for i in 0..simdlen..(lastindex - 1) do
      Vector.Widen(Vector<int>(xs, i), &l1, &l2)
      subtotal <- Vector.Add (subtotal, l1)
      subtotal <- Vector.Add (subtotal, l2)

    let mutable total = 0L
    for i = 0 to Vector<int64>.Count - 1 do
      total <- total + subtotal[i]

    for i = lastindex to xs.Length - 1 do
      total <- total + int64 xs[i]

    total

  [<Benchmark>]
  member __.Array_sum() = Array.sum xs
  
  [<Benchmark>]
  member __.System_Linq_Sum() = xs.Sum()

[<EntryPoint>]
let main args =
  BenchmarkRunner.Run<Benchmark>() |> ignore
  0