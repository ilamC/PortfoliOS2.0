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
    e.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', function (e) {
    e.respondWith((async function () {
      // Pages go network-first so an updated index.html reaches people who visited before.
      if (e.request.mode === 'navigate') {
        try {
          const fresh = await fetch(e.request);
          const cache = await caches.open(cacheName);
          cache.put(e.request, fresh.clone());
          return fresh;
        } catch (err) {
          const cached = await caches.match(e.request);
          if (cached) { return cached; }
          throw err;
        }
      }

      let response = await caches.match(e.request);
      console.log(`[Service Worker] Fetching resource: ${e.request.url}`);
      if (response) { return response; }

      response = await fetch(e.request);
      const cache = await caches.open(cacheName);
      console.log(`[Service Worker] Caching new resource: ${e.request.url}`);
      cache.put(e.request, response.clone());
      return response;
    })());
});
