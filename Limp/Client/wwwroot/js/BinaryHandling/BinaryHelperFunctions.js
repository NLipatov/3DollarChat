"use strict";

function createBlobUrl(byteArray, mimeType) {
    var blob = new Blob([byteArray], { type: mimeType });
    return URL.createObjectURL(blob);
}

function startStream(){
    var video = document.getElementById('video');
    console.log("video" + video)
    console.log("supported" + Hls.isSupported())
    if (Hls.isSupported()) {
        var hls = new Hls({
            debug: true,
        });
        hls.loadSource('https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8');
        hls.attachMedia(video);
        hls.on(Hls.Events.MEDIA_ATTACHED, function () {
            video.muted = true;
            video.play();
        });
    }
        // hls.js is not supported on platforms that do not have Media Source Extensions (MSE) enabled.
        // When the browser has built-in HLS support (check using `canPlayType`), we can provide an HLS manifest (i.e. .m3u8 URL) directly to the video element through the `src` property.
    // This is using the built-in support of the plain video element, without using hls.js.
    else if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = 'https://test-streams.mux.dev/x36xhzz/x36xhzz.m3u8';
        video.addEventListener('canplay', function () {
            video.play();
        });
    }
}