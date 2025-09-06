# TailwindCSS.Blazor

A .NET library that integrates Tailwind CSS CLI with Blazor applications, featuring automatic hot-reload functionality.

## How it works

This package implements hot-reload by fetching CSS and injecting it into a style tag while removing the old link tag.
Sometimes when viewing the Elements panel in Chrome, these style tags may not be visible because when .NET hot reload is
working correctly, it overwrites the header - this doesn't affect functionality.

Additionally, if .NET hot reload isn't working, this package will continue creating new style tags ensuring that app.css
hot reload still functions properly.

## Features
- Hot-reload support for development on blazor Interactive Auto mode

## Installation
```
## Project Structure Overview

Blazor1/
├─ Blazor1/
│  ├─ Blazor1.csproj    <--- 4 intall nuget package
│  ├─ app.css           <--- 1 add this file
│  ├─ Component/
│  │  └─ App.razor      <--- 5,6 add style & hot reload script
│  │....
│  ├─ Program.cs        <--- 7 add code for run cli & integrated hot reload when run
│  └─ wwwroot
├─ Blazor1.Client/
│  ├─ Blazor1.Client.csproj
│  │....
│  └─ wwwroot
├─ tailwind.config.js   <---- 2 add this file
├─ package.json         <---- 3 install npm package 
└─ node_modules/
```


1. add new `app.css` file to a root project and fill the following code
```css
@import "tailwindcss";
```
2. At parent folder (folder that contain folder Blazor1 and Blazor1.Client)  add `tailwind.config.js` file to a root project and fill the following code


```js
module.exports = {
    content: [
        "./**/*.html",
        "./**/*.razor",
        "./**/*.cshtml",
        "./**/*.js",
        "./**/*.ts",
    ],
    theme: {
        extend: {},
    },
    plugins: [],
}
```
3. Ensure `nodejs` is installed. At root install package
```shell
npm install tailwindcss @tailwindcss/cli
```
4. Install this package via NuGet 
```shell
dotnet add package TailwindCSS.Blazor
```
5. At `App.razor` file, add `<link rel="stylesheet" href="@Assets["css/app.css"]"/>` before `</head>` tag 
```html
<head>
    ....
    <link rel="stylesheet" href='@Assets['app.css"]'/>  <!-- add this line -->
</head>
```
6. At `App.razor` file, add `<script src="_content/TailwindCSS.Blazor/tailwindcss-blazor-hot-reload.js"></script>` before `</body>` tag
```html
<body>
    ....
    <script src="_framework/blazor.web.js"></script>
    <script src="_content/TailwindCSS.Blazor/tailwindcss-blazor-hot-reload.js"></script>  <!-- add this line -->
</body>
```
7. At `Program.cs` of web project add two lines
```csharp
builder.AddTailwindCSS(); // <-- add this line

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseTailwindCSS(); // <-- add this line
    app.UseWebAssemblyDebugging();
}
```
8. Run a Blazor project. (can run with F5 or by `dotnet run` command)
9. Change `app.css` file or razor file. The tailwind-cli will output to `wwwroot/css/app.css` and it will auto hot reload via websocket.

## Tailwind Configuration

You can configure Tailwind CSS settings in the `tailwind.config.js` file.
