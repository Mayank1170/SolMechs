import './globals.css'
import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import { WalletProviders } from './providers'
import React from 'react'

const inter = Inter({ subsets: ['latin'] })

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
    <html lang="en">
      <body className={inter.className}>
        <WalletProviders>
          {children}
        </WalletProviders>
      </body>
    </html>
  )
}