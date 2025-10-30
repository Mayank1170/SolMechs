'use client'

import React, { useState } from 'react';
import { AVAILABLE_DRONES, DroneType } from '../utils/droneData';

interface DroneSelectionModalProps {
  isOpen: boolean;
  onSelect: (droneId: string) => void;
  onClose: () => void;
}

export default function DroneSelectionModal({ isOpen, onSelect, onClose }: DroneSelectionModalProps) {
  const [selectedDrone, setSelectedDrone] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleConfirm = () => {
    if (selectedDrone) {
      onSelect(selectedDrone);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/80 flex items-center justify-center z-50 p-4">
      <div className="bg-gradient-to-br from-gray-900 to-gray-800 rounded-lg border-2 border-purple-500/50 max-w-4xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-gradient-to-r from-purple-900 to-pink-900 p-4 border-b border-purple-500/30">
          <h2 className="text-2xl font-bold text-white text-center">Choose Your Drone</h2>
          <p className="text-gray-300 text-sm text-center mt-1">
            Select which drone will fight the streamer&apos;s mech on your behalf
          </p>
        </div>

        {/* Drone Grid */}
        <div className="p-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {AVAILABLE_DRONES.map((drone) => {
            const isDisabled = drone.id === 'drone6';

            return (
              <div
                key={drone.id}
                onClick={() => !isDisabled && setSelectedDrone(drone.id)}
                className={`
                  rounded-lg border-2 p-4 transition-all relative
                  ${isDisabled
                    ? 'opacity-50 cursor-not-allowed border-gray-700 bg-gray-900/50'
                    : 'cursor-pointer transform hover:scale-105'
                  }
                  ${!isDisabled && selectedDrone === drone.id
                    ? 'border-purple-500 bg-purple-900/30 shadow-lg shadow-purple-500/50'
                    : !isDisabled && 'border-gray-600 bg-gray-800/50 hover:border-purple-400'
                  }
                `}
              >

                <div className="aspect-video bg-gradient-to-br from-gray-700 to-gray-900 rounded-lg mb-3 flex items-center justify-center border border-gray-600">

                  <img src={drone.image} alt={drone.name} className="w-full h-full object-cover rounded-lg" />
                </div>

                {/* Drone Info */}
                <h3 className="text-xl font-bold text-white mb-1">{drone.name}</h3>

                {/* Stats */}


                {/* Disabled Badge */}
                {isDisabled && (
                  <div className="absolute top-2 right-2 bg-red-600 text-white px-3 py-1 rounded-full text-xs font-bold">
                    COMING SOON
                  </div>
                )}

                {/* Selected Indicator */}
                {selectedDrone === drone.id && !isDisabled && (
                  <div className="mt-3 text-center text-purple-400 font-bold text-sm">
                    ✓ SELECTED
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-gradient-to-r from-gray-900 to-gray-800 p-4 border-t border-purple-500/30 flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 py-3 bg-gray-700 hover:bg-gray-600 text-white font-bold rounded transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={!selectedDrone}
            className="flex-1 py-3 bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 disabled:from-gray-600 disabled:to-gray-600 disabled:cursor-not-allowed text-white font-bold rounded transition-all transform hover:scale-105 disabled:hover:scale-100"
          >
            {selectedDrone ? 'CONFIRM SELECTION' : 'SELECT A DRONE'}
          </button>
        </div>
      </div>
    </div>
  );
}
