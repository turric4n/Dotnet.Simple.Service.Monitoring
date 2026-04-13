export function KythrLogo({ className = 'h-6 w-6' }: { className?: string }) {
  return (
    <svg
      className={className}
      viewBox="0 0 512 512"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <defs>
        <linearGradient id="kythr-bg" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#6366f1" />
          <stop offset="100%" stopColor="#8b5cf6" />
        </linearGradient>
        <linearGradient id="kythr-pulse" x1="0%" y1="50%" x2="100%" y2="50%">
          <stop offset="0%" stopColor="#e0e7ff" />
          <stop offset="50%" stopColor="#ffffff" />
          <stop offset="100%" stopColor="#e0e7ff" />
        </linearGradient>
      </defs>
      <rect width="512" height="512" rx="96" fill="url(#kythr-bg)" />
      <path
        d="M256 72c-72 0-144 32-144 32v176c0 88 144 160 144 160s144-72 144-160V104S328 72 256 72z"
        fill="none"
        stroke="white"
        strokeWidth="16"
        strokeLinejoin="round"
        opacity="0.2"
      />
      <polyline
        points="100,280 180,280 210,200 240,360 270,180 300,320 330,260 360,280 412,280"
        fill="none"
        stroke="url(#kythr-pulse)"
        strokeWidth="20"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <text
        x="256"
        y="180"
        fontFamily="Inter, system-ui, sans-serif"
        fontSize="120"
        fontWeight="800"
        fill="white"
        textAnchor="middle"
        dominantBaseline="central"
        opacity="0.9"
      >
        K
      </text>
    </svg>
  );
}
