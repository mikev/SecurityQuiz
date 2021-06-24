Run Instructions

The exe named "SecurityQuiz.exe" is located in "bin/Debug/net5.0"
It requires Windows.  Running the exe will launch a web service running on port 9000.

Requirements

https://gist.github.com/amissemer/8314efe851f979670f026c10161267f6

Missed Requirements

The following requirements were not delivered in this MVP.

1. Must return results within 2 seconds
The provided Stage API at https://incident-api.use1stag.elevatesecurity.io/incidents/ is taking longer than 2 seconds to respond.
Thus my API takes longer than 2 seconds to respond.
A reasonable implementation solution is to cache the intermediate API results.  Conceptualy easy, but it does require setting up a local cache.  It
also involves design decisions about merging or clearing existing data.  Future improvement...

2. Output Format
My output almost matches the spec.  Howver, the spec was vague about whether the json is normalized or exactly matches the source API data.  I did normalize the output.
Also, to keep the code generic, I always normalize the incident fieldnames json, e.g. with the field name ""machine_ip" for example
This may not be what the spec author intended.  My solution is written in C# which strongly types the data.  Passing the json data through
is easier in a dynamically typed language, comnpared to C#.

From a design and product perspective a normalized API would be better than exposing the underlying API data structures.  In any case this
should be clarified.

3. Added json string
My json includes "Data" in the output.  I didn't have time to remove this extraneous json string.