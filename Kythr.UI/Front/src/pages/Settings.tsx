import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { useSettingsStore } from '@/stores/settingsStore';
import { useThemeStore } from '@/stores/themeStore';
import { Settings as SettingsIcon, RotateCcw, Monitor, Moon, Sun } from 'lucide-react';

export default function Settings() {
  const settings = useSettingsStore();
  const { theme, setTheme } = useThemeStore();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Settings</h1>
        <p className="text-muted-foreground">Configure the monitoring dashboard</p>
      </div>

      {/* Appearance */}
      <Card>
        <CardHeader>
          <CardTitle>Appearance</CardTitle>
          <CardDescription>Customize the look and feel</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <label className="text-sm font-medium">Theme</label>
            <div className="flex gap-2 mt-2">
              {[
                { value: 'light' as const, label: 'Light', icon: Sun },
                { value: 'dark' as const, label: 'Dark', icon: Moon },
                { value: 'system' as const, label: 'System', icon: Monitor },
              ].map(({ value, label, icon: Icon }) => (
                <Button
                  key={value}
                  variant={theme === value ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => setTheme(value)}
                >
                  <Icon className="mr-2 h-4 w-4" />
                  {label}
                </Button>
              ))}
            </div>
          </div>
          <Separator />
          <div>
            <label className="text-sm font-medium">Display Density</label>
            <div className="flex gap-2 mt-2">
              {(['comfortable', 'compact'] as const).map((d) => (
                <Button
                  key={d}
                  variant={settings.displayDensity === d ? 'default' : 'outline'}
                  size="sm"
                  onClick={() => settings.setDisplayDensity(d)}
                >
                  {d.charAt(0).toUpperCase() + d.slice(1)}
                </Button>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Data */}
      <Card>
        <CardHeader>
          <CardTitle>Data & Refresh</CardTitle>
          <CardDescription>Control data refresh intervals and table behavior</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-3">
            <div>
              <label className="text-sm font-medium">Refresh Interval (seconds)</label>
              <Input
                type="number"
                min={5}
                max={300}
                value={settings.refreshInterval / 1000}
                onChange={(e) => settings.setRefreshInterval(Number(e.target.value) * 1000)}
                className="mt-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">Default Time Range (hours)</label>
              <Input
                type="number"
                min={1}
                max={720}
                value={settings.defaultTimeRange}
                onChange={(e) => settings.setDefaultTimeRange(Number(e.target.value))}
                className="mt-2"
              />
            </div>
            <div>
              <label className="text-sm font-medium">Table Page Size</label>
              <Input
                type="number"
                min={5}
                max={100}
                value={settings.tablePageSize}
                onChange={(e) => settings.setTablePageSize(Number(e.target.value))}
                className="mt-2"
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Notifications */}
      <Card>
        <CardHeader>
          <CardTitle>Notifications</CardTitle>
          <CardDescription>Configure alert notifications</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Enable Notifications</p>
              <p className="text-sm text-muted-foreground">Show browser notifications for status changes</p>
            </div>
            <Button
              variant={settings.notificationsEnabled ? 'default' : 'outline'}
              size="sm"
              onClick={() => settings.setNotificationsEnabled(!settings.notificationsEnabled)}
            >
              {settings.notificationsEnabled ? 'Enabled' : 'Disabled'}
            </Button>
          </div>
          <Separator />
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">Notification Sound</p>
              <p className="text-sm text-muted-foreground">Play a sound when a status change occurs</p>
            </div>
            <Button
              variant={settings.notificationSound ? 'default' : 'outline'}
              size="sm"
              onClick={() => settings.setNotificationSound(!settings.notificationSound)}
            >
              {settings.notificationSound ? 'Enabled' : 'Disabled'}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Reset */}
      <div className="flex justify-end">
        <Button variant="outline" onClick={settings.resetToDefaults}>
          <RotateCcw className="mr-2 h-4 w-4" />
          Reset to Defaults
        </Button>
      </div>
    </div>
  );
}
