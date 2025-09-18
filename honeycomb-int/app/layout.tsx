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
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className={mekMono.variable}>
      <head>
        <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no" />
        <meta name="apple-mobile-web-app-capable" content="yes" />
        <meta name="apple-mobile-web-app-status-bar-style" content="black-translucent" />
        <meta name="mobile-web-app-capable" content="yes" />
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