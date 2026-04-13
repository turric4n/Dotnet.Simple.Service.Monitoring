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
          <stop offset="0%" stopColor="#4338ca" />
          <stop offset="50%" stopColor="#6d28d9" />
          <stop offset="100%" stopColor="#7e22ce" />
        </linearGradient>
        <linearGradient id="kythr-shield" x1="50%" y1="0%" x2="50%" y2="100%">
          <stop offset="0%" stopColor="white" stopOpacity="0.18" />
          <stop offset="100%" stopColor="white" stopOpacity="0.03" />
        </linearGradient>
        <linearGradient id="kythr-pulse" x1="0%" y1="50%" x2="100%" y2="50%">
          <stop offset="0%" stopColor="#c4b5fd" stopOpacity="0.4" />
          <stop offset="20%" stopColor="#ffffff" />
          <stop offset="80%" stopColor="#ffffff" />
          <stop offset="100%" stopColor="#c4b5fd" stopOpacity="0.4" />
        </linearGradient>
        <linearGradient id="kythr-ring" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#a5b4fc" stopOpacity="0.5" />
          <stop offset="50%" stopColor="#e0e7ff" stopOpacity="0.2" />
          <stop offset="100%" stopColor="#a5b4fc" stopOpacity="0.5" />
        </linearGradient>
        <filter id="kythr-glow">
          <feGaussianBlur stdDeviation="8" result="blur" />
          <feMerge>
            <feMergeNode in="blur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
        <filter id="kythr-glow-sm">
          <feGaussianBlur stdDeviation="3" result="blur" />
          <feMerge>
            <feMergeNode in="blur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>
      <rect width="512" height="512" rx="112" fill="url(#kythr-bg)" />
      <circle cx="180" cy="160" r="220" fill="white" opacity="0.035" />
      <ellipse
        cx="256" cy="260" rx="190" ry="190"
        fill="none" stroke="url(#kythr-ring)" strokeWidth="1.5"
        strokeDasharray="8 12" opacity="0.6"
      />
      <path
        d="M256 88c-76 0-140 30-140 30v168c0 46 26 88 64 120s76 50 76 50 38-18 76-50 64-74 64-120V118S332 88 256 88z"
        fill="url(#kythr-shield)"
        stroke="white"
        strokeWidth="8"
        strokeLinejoin="round"
        strokeOpacity="0.3"
      />
      <polyline
        points="108,272 184,272 210,272 230,222 252,340 274,196 294,312 310,254 326,272 404,272"
        fill="none"
        stroke="url(#kythr-pulse)"
        strokeWidth="16"
        strokeLinecap="round"
        strokeLinejoin="round"
        filter="url(#kythr-glow)"
      />
      <circle cx="274" cy="196" r="6" fill="white" filter="url(#kythr-glow-sm)" />
    </svg>
  );
}
