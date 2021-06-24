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

2. Added json string
My json includes a key string "Data" in the output.  I didn't have time to remove this extraneous json key string.

All my incident data is normalized to this format

                {
                "type": "executable"
                "priority": "critical",
                "machine_ip": "17.99.238.86",
                "timestamp": 1500020421.9333,
                }

The problem specifies normalized data, but this exact format should be clarified.