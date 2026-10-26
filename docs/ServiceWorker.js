const cacheName = "DefaultCompany-portfoliOS-0.1.0";
const contentToCache = [
    "Build/themain.loader.js",
    "Build/themain.framework.js.unityweb",
    "Build/themain.data.unityweb",
    "Build/themain.wasm.unityweb",
    "TemplateData/style.css"

];

self.addEventListener('install', function (e) {
    console.log('[Service Worker] Install');
    
    e.waitUntil((async function () {
      const cache = await caches.open(cacheName);
      console.log('[Service Worker] Caching all: app shell and content');
      await cache.addAll(contentToCache);
    })());
    self.skipWaiting();
});

self.addEventListener('activate', function (e) {
    // Drop caches left by older builds.
    e.waitUntil((async function () {
      const names = await caches.keys();
      await Promise.all(names.filter(function (n) { return n !== cacheName; }).map(function (n) { return caches.delete(n); }));
      await self.clients.claim();
    })());
});

// Network first: a new build uploaded under the same file names reaches people who visited
// before (the browser's HTTP cache keeps repeat visits cheap). The cache is only an offline fallback.
self.addEventListener('fetch', function (e) {
    if (e.request.method !== 'GET') { return; }
    e.respondWith((async function () {
      try {
        const response = await fetch(e.request);
        if (response.ok) {
          const cache = await caches.open(cacheName);
          cache.put(e.request, response.clone());
        }
        return response;
      } catch (err) {
        const cached = await caches.match(e.request);
        if (cached) { return cached; }
        throw err;
      }
    })());
});
