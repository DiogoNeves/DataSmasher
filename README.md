Distributed Data Crunching/Smashing (if it goes wrong)

This is a distributed Job execution system with a simple stats HTTP server.
Jobs are basically classes that get either executed by the local machine or sent to a different machine for execution and later returned with the result.

When of my main 'find out later' is how to serialise closures in .NET 3.5 (maybe Tasks work well in .NET 4?)

I'll be adding more execution instructions soon.

P.s. There's some code (object serialisation etc) that I removed while refactoring the project (it was just test code). Look at changes to find it...
