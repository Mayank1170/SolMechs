import React from 'react';
import Image from 'next/image';

interface CyberpunkButtonProps {
  children: React.ReactNode;
  onClick?: () => void;
  disabled?: boolean;
  className?: string;
  width?: number;
  height?: number;
  textSize?: string;
}

export default function CyberpunkButton({ 
  children, 
  onClick, 
  disabled = false,
  className = '',
  width = 200,
  height = 60,
  textSize = 'text-lg'
}: CyberpunkButtonProps) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`relative hover:scale-105 transition-transform duration-200 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 ${className}`}
    >
      <Image 
        src="/images/Button1.png" 
        alt="Button" 
        width={width} 
        height={height} 
        className="w-auto h-auto"
      />
      <span className={`absolute inset-0 flex items-center justify-center text-white font-bold ${textSize}`}>
        {children}
      </span>
    </button>
  );
}