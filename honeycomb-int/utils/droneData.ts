
export interface DroneType {
  id: string;
  name: string;
  image: string;
  stats: {
    speed: number;
    armor: number;
    firepower: number;
  };
}

export const AVAILABLE_DRONES: DroneType[] = [
  {
    id: 'drone1',
    name: 'Titan',
    image: '/images/1.png',
    stats: {
      speed: 7,
      armor: 6,
      firepower: 7
    }
  },
  {
    id: 'drone2',
    name: 'Striker',
    image: '/images/2.png',
    stats: {
      speed: 5,
      armor: 9,
      firepower: 6
    }
  },
  {
    id: 'drone3',
    name: 'Arclight',
    image: '/images/3.png',
    stats: {
      speed: 10,
      armor: 4,
      firepower: 6
    }
  },
  {
    id: 'drone4',
    name: 'HeartCore',
    image: '/images/4.png',
    stats: {
      speed: 5,
      armor: 5,
      firepower: 10
    }
  },
  {
    id: 'drone5',
    name: 'Solus',
    image: '/images/5.png',
    stats: {
      speed: 8,
      armor: 5,
      firepower: 8
    }
  },
  {
    id: 'drone6',
    name: 'Base',
    image: '/images/6.png',
    stats: {
      speed: 7,
      armor: 6,
      firepower: 9
    }
  }
];

// Get drone by ID
export function getDroneById(id: string): DroneType | undefined {
  return AVAILABLE_DRONES.find(drone => drone.id === id);
}
