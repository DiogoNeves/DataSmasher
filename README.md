# Data Smasher

**DataSmasher is a distributed, multi-threaded, job execution framework.** Jobs are a generic concept, they can compile code, crunch data, search data and the list goes on...
DataSmasher exists because I wanted to learn more about distributed and multi-threaded software development and doesn't have a big plan (yet), but I want to keep it alive for now (it's quite fun).

I hope you like it. Any feedback, requests, critiques or general “Hey, how's it going?” comments are welcomed±.

Have a look at the [Wiki pages](http://github.com/DiogoNeves/DataSmasher/wiki) for more information, documentation and general ideas (which I will add soon!)

± This isn't a complete list of accepted comments ;)


## Instructions

Coming soon...


## Present day...

### Pros

* Simple to use, create a job, subscribe to its events and Enqueue in the `JobManager`
* Jobs are completely generic
* Modular architecture, should be very very simple to extend the distribution model±, job management or anything else
* Web interface

### Cons

* Just a few days worth of work, very very early stages and basic!
* Too generic, needs specific implementations of distributed models to be useful

± Just extend the `RemoteJobConsumer` class


## (Unsorted) Future work list

* Blog post about the tech side!
* Add Tests! (Yep, should have done first)
* Fix node addresses, at the moment always returns 127.0.0.1 **Next task!**
* Make the server suck less! (many aspects of it are hardcoded) **Next task!**
* Error handling, especially sudden disconnections, definitely has to be improved
* Select serialisation or marshalling of Jobs depending on the remote node platform
* C++ and maybe Java (Android) clients
* Limit the local number of jobs depending on the current platform (LocalJobConsumer is independent of this, just give it a max number of threads)
* RemoteJobConumers will have to accept more than 1 connection and 1 job
* Make nodes independent of WebService to discover other nodes in the cluster (NodeGraph)
* Create Job dependency manager

