# UMD2MKV
Convert a PSP UMD movie iso file to a MKV file (video,audio,subs)
![App Screenshot](screenshot.jpg)




Idea/general steps needed coming from:  
https://www.journaldulapin.com/2015/02/12/comment-ripper-un-umd-video/  
https://www.reddit.com/r/PSP/comments/n7t7co/my_quest_to_rip_a_psp_umd_movie/  

Extracting mps stream from UMD iso file using DiscUtils:  
https://github.com/DiscUtils/DiscUtils   

Demuxing audio streams and subtitles based on VGMToolbox code (reduced and cleaned up):  
https://github.com/Manicsteiner/VGMToolbox  

Extracting PNG files from subtitle files based on:  
https://gist.github.com/rlaphoenix/c2547539f6b35aa7dd33714c43813150  

Encoding audio and Muxing by using FFmpeg (through Xabe.Ffmpeg):  
https://github.com/FFmpeg/FFmpeg. 
https://github.com/tomaszzmuda/Xabe.FFmpeg  


