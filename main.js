const { app, BrowserWindow } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

let mainWindow;
let dotnetProcess;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 800,
    height: 600,
    webPreferences: {
      nodeIntegration: true,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js')
    },
    icon: path.join(__dirname, 'UnityCacheCleaner/Resources/AppIcon.ico')
  });

  // Start the .NET application
  const exePath = process.platform === 'win32' 
    ? path.join(__dirname, 'dist/win/UnityCacheCleaner.exe')
    : path.join(__dirname, 'dist/linux/UnityCacheCleaner');

  dotnetProcess = spawn(exePath);

  dotnetProcess.stdout.on('data', (data) => {
    console.log(`stdout: ${data}`);
  });

  dotnetProcess.stderr.on('data', (data) => {
    console.error(`stderr: ${data}`);
  });

  dotnetProcess.on('close', (code) => {
    console.log(`Child process exited with code ${code}`);
    app.quit();
  });
}

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    if (dotnetProcess) {
      dotnetProcess.kill();
    }
    app.quit();
  }
});

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});
