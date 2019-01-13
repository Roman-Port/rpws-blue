# RPWS Blue Server

This is the main server that handles RPWS requests. **Some parts are written very quickly and are very gross. It's partially being rewritten right now.**

## What is RPWS?
RPWS is a Pebble server replacement service, written in C#. It aims to replace the old Pebble services that **ended Thursday, July 19th, 2018**. RPWS lives at https://get-rpws.com/.

### Why is there a special branch of Kestrel here?
You must use your own build of Kestrel and disable the content-length check on PUT requests. This is because the Android Pebble client breaks spec and sends a PUT request without the content-length header when putting apps in the Locker. 