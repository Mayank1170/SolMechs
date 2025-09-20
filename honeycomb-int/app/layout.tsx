import './globals.css'
import type { Metadata } from 'next'
import localFont from 'next/font/local'
import { WalletProviders } from './providers'
import React from 'react'

const mekMono = localFont({
  src: '../public/font/MEK-Mono.otf',
  display: 'swap',
  variable: '--font-mek-mono'
})

export const metadata: Metadata = {
  title: 'SolMechs',
  description: '',
  manifest: '/manifest.json',
  appleWebApp: {
    capable: true,
    statusBarStyle: 'black-translucent',
    title: 'SolMechs'
  },
  icons: {
    icon: [
      { url: '/icons/icon-192x192.png', sizes: '192x192', type: 'image/png' },
      { url: '/icons/icon-512x512.png', sizes: '512x512', type: 'image/png' },
    ],
    apple: [
      { url: '/icons/icon-152x152.png', sizes: '152x152', type: 'image/png' },
      { url: '/icons/icon-192x192.png', sizes: '192x192', type: 'image/png' },
    ],
  },
  other: {
    'mobile-web-app-capable': 'yes',
    'apple-mobile-web-app-capable': 'yes',
    'apple-mobile-web-app-status-bar-style': 'black-translucent',
    'apple-mobile-web-app-title': 'SolMechs',
    'application-name': 'SolMechs',
    'msapplication-TileColor': '#1a1a2e',
    'msapplication-TileImage': '/icons/icon-144x144.png',
  },
}

export function generateViewport() {
  return {
    themeColor: '#1a1a2e',
    width: 'device-width',
    initialScale: 1,
    maximumScale: 1,
    userScalable: false,
    viewportFit: 'cover',
  }
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className={mekMono.variable}>
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover" />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
        <meta name="mobile-web-app-capable" content="yes" />
        <link rel="apple-touch-icon" href="/icons/icon-192x192.png" />
        <link rel="apple-touch-startup-image" href="/icons/icon-512x512.png" />
        <script
          dangerouslySetInnerHTML={{
            __html: `
              if ('serviceWorker' in navigator) {
                window.addEventListener('load', function() {
                  navigator.serviceWorker.register('/sw.js', { scope: '/' })
                    .then(function(registration) {
                      console.log('🎮 SW: Service Worker registered successfully:', registration.scope);
                    })
                    .catch(function(error) {
                      console.log('❌ SW: Service Worker registration failed:', error);
                    });
                });
              }
            `,
          }}
        />
      </head>
      <body className={`${mekMono.className} font-mek bg-cover bg-center bg-no-repeat min-h-screen overflow-x-hidden`} 
            style={{backgroundImage: "url('/images/bg.png')", backgroundAttachment: 'fixed'}}>
        <WalletProviders>
          {children}
        </WalletProviders>
      </body>
    </html>
  )
}