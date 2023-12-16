Good day! The project called TwitterX was written in the asp .net core8 framework. This project demonstrates a simple social network pub/sub system, with short text messages being sent between users. Publishers send out short "Chirp" messages to any other users that are following them.

The project consists of two applications, TwitterX.Server which hosts Orleans and all of the grains, and TwitterX.Client which hosts an Orleans client, and the api requests seen in the above screenshot.
![image](https://github.com/yergesh/TwitterX/assets/54017134/cc3588e4-cbc3-4c1d-8a89-9e5e1fbd6247)
Authorization using JWT tokens has been implemented.

https://localhost:7026/auth
![image](https://github.com/yergesh/TwitterX/assets/54017134/11d40283-f0f4-424d-9113-c5b50014bcd4)

![image](https://github.com/yergesh/TwitterX/assets/54017134/a8cabbe3-c996-4c70-bed8-8a3919712800)

After authorization, all methods are available

https://localhost:7026/post?message=first%20message
![image](https://github.com/yergesh/TwitterX/assets/54017134/6e3d94d9-2cbc-4337-b91c-0f386a2703ec)

https://localhost:7026/like?postId=0c744fec-5b36-451f-a290-7c9e0e8512e6
![image](https://github.com/yergesh/TwitterX/assets/54017134/2fcf92b8-6fd7-4142-8928-602ce5241331)

https://localhost:7026/posts/10/0 (all posts)
![image](https://github.com/yergesh/TwitterX/assets/54017134/828a3167-c845-46ce-a529-90f3c71ec60f)

and other methods
