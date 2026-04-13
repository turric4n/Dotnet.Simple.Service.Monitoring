import { useConnectionStore } from '@/stores/connectionStore';
import { StatusDot } from './StatusDot';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { formatDateTime } from '@/lib/utils';

const labelMap: Record<string, string> = {
  connected: 'Connected',
  reconnecting: 'Reconnecting...',
  disconnected: 'Disconnected',
};

export function ConnectionStatus() {
  const { state, lastConnected } = useConnectionStore();
  const label = labelMap[state] ?? 'Unknown';

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <div className="flex items-center gap-2 text-sm">
          <StatusDot status={state} size="sm" animate={state !== 'connected'} />
          <span className="hidden sm:inline text-muted-foreground">{label}</span>
        </div>
      </TooltipTrigger>
      <TooltipContent>
        <p>Status: {label}</p>
        <p className="text-xs text-muted-foreground">Hub: /monitoringhub</p>
        {lastConnected && (
          <p className="text-xs text-muted-foreground">Last connected: {formatDateTime(lastConnected)}</p>
        )}
      </TooltipContent>
    </Tooltip>
  );
}
