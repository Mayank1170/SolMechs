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
  title: 'Honeycomb Integration',
  description: 'Honeycomb Protocol Integration App',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className={mekMono.variable}>
      <body className={`${mekMono.className} font-mek bg-cover bg-center bg-no-repeat min-h-screen`} style={{backgroundImage: "url('/images/bg.png')"}}>
        <WalletProviders>
          {children}
        </WalletProviders>
      </body>
    </html>
  )
}