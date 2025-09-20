const CACHE_NAME = 'solmechs-v1.0.0'
const RUNTIME_CACHE = 'solmechs-runtime'

// Assets to cache immediately
const PRECACHE_ASSETS = [
  '/',
  '/manifest.json',
  '/images/logo.svg',
  '/images/bg.png',
  '/images/Button1.png',
  '/images/connect_logo.png',
  '/images/frame.png',
  '/font/MEK-Mono.otf',
  '/_next/static/css/app/layout.css'
]

// Unity game assets (cache when accessed)
const UNITY_ASSETS = [
  '/unity/Build/webgl-build.data',
  '/unity/Build/webgl-build.framework.js',
  '/unity/Build/webgl-build.loader.js',
  '/unity/Build/webgl-build.wasm',
  '/unity/unity-logo-dark.png',
  '/unity/progress-bar-empty-dark.png',
  '/unity/progress-bar-full-dark.png',
  '/unity/OverlayPSG1.png'
]

// Install event - cache essential assets
self.addEventListener('install', (event) => {
  console.log('🔧 SW: Installing service worker...')
  
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('📦 SW: Caching essential assets')
        return cache.addAll(PRECACHE_ASSETS.map(url => new Request(url, {
          cache: 'reload'
        })))
      })
      .then(() => {
        console.log('✅ SW: Service worker installed successfully')
        return self.skipWaiting()
      })
      .catch((error) => {
        console.error('❌ SW: Installation failed:', error)
      })
  )
})

// Activate event - cleanup old caches
self.addEventListener('activate', (event) => {
  console.log('🚀 SW: Activating service worker...')
  
  event.waitUntil(
    caches.keys()
      .then((cacheNames) => {
        return Promise.all(
          cacheNames.map((cacheName) => {
            if (cacheName !== CACHE_NAME && cacheName !== RUNTIME_CACHE) {
              console.log('🗑️ SW: Deleting old cache:', cacheName)
              return caches.delete(cacheName)
            }
          })
        )
      })
      .then(() => {
        console.log('✅ SW: Service worker activated')
        return self.clients.claim()
      })
  )
})

// Fetch event - serve from cache or network
self.addEventListener('fetch', (event) => {
  const { request } = event
  const url = new URL(request.url)
  
  // Skip non-GET requests and chrome-extension
  if (request.method !== 'GET' || url.protocol === 'chrome-extension:') {
    return
  }
  
  // Handle different types of requests
  if (url.pathname.startsWith('/unity/')) {
    // Unity assets - cache first, then network
    event.respondWith(cacheFirstStrategy(request))
  } else if (url.pathname.startsWith('/_next/static/')) {
    // Next.js static assets - cache first
    event.respondWith(cacheFirstStrategy(request))
  } else if (url.pathname.startsWith('/images/') || url.pathname.startsWith('/icons/')) {
    // Images - cache first
    event.respondWith(cacheFirstStrategy(request))
  } else if (url.pathname === '/' || url.pathname.startsWith('/unity')) {
    // HTML pages - network first with cache fallback
    event.respondWith(networkFirstStrategy(request))
  } else if (url.origin !== self.location.origin) {
    // External requests - network only (for APIs, wallets, etc.)
    return
  } else {
    // Other requests - network first
    event.respondWith(networkFirstStrategy(request))
  }
})

// Cache-first strategy (for static assets)
async function cacheFirstStrategy(request) {
  try {
    const cachedResponse = await caches.match(request)
    if (cachedResponse) {
      return cachedResponse
    }
    
    const networkResponse = await fetch(request)
    if (networkResponse.ok) {
      const cache = await caches.open(RUNTIME_CACHE)
      cache.put(request, networkResponse.clone())
    }
    return networkResponse
  } catch (error) {
    console.log('📡 SW: Cache-first strategy failed:', error)
    return new Response('Offline content not available', { 
      status: 503,
      statusText: 'Service Unavailable' 
    })
  }
}

// Network-first strategy (for dynamic content)
async function networkFirstStrategy(request) {
  try {
    const networkResponse = await fetch(request)
    if (networkResponse.ok) {
      const cache = await caches.open(RUNTIME_CACHE)
      cache.put(request, networkResponse.clone())
    }
    return networkResponse
  } catch (error) {
    console.log('📡 SW: Network failed, trying cache:', request.url)
    const cachedResponse = await caches.match(request)
    if (cachedResponse) {
      return cachedResponse
    }
    
    // Return offline page for navigation requests
    if (request.mode === 'navigate') {
      return caches.match('/') || new Response('App is offline', {
        headers: { 'Content-Type': 'text/html' }
      })
    }
    
    return new Response('Offline', { 
      status: 503,
      statusText: 'Service Unavailable' 
    })
  }
}

// Background sync for when connection is restored
self.addEventListener('sync', (event) => {
  console.log('🔄 SW: Background sync triggered:', event.tag)
  
  if (event.tag === 'background-sync') {
    event.waitUntil(
      // You can add background sync logic here
      // For example: sync game progress, pending transactions, etc.
      Promise.resolve()
    )
  }
})

// Push notifications (for future features)
self.addEventListener('push', (event) => {
  if (event.data) {
    const data = event.data.json()
    console.log('📱 SW: Push notification received:', data)
    
    const options = {
      body: data.body || 'New update available!',
      icon: '/icons/icon-192x192.png',
      badge: '/icons/icon-72x72.png',
      vibrate: [200, 100, 200],
      tag: 'solmechs-notification',
      actions: [
        {
          action: 'open',
          title: 'Open Game'
        },
        {
          action: 'close',
          title: 'Close'
        }
      ]
    }
    
    event.waitUntil(
      self.registration.showNotification(data.title || 'SolMechs', options)
    )
  }
})

// Handle notification clicks
self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  
  if (event.action === 'open') {
    event.waitUntil(
      clients.openWindow('/')
    )
  }
})

console.log('🎮 SolMechs Service Worker loaded!')