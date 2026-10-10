// Reproduce all raster assets from the approved vector mark.
// Usage: node scripts/Export-BrandIcons.cjs <sharp-module-path>
const fs = require('node:fs');
const path = require('node:path');
const sharp = require(process.argv[2] || 'sharp');
const output = path.resolve(__dirname, '../src/ZonyLrcTools.Desktop/Assets');
async function main() {
  const input = fs.readFileSync(path.join(output, 'logo-app.svg'));
  const sizes = [16,20,24,32,40,48,64,128,256,512];
  const buffers = new Map();
  for (const size of sizes) {
    const png = await sharp(input).resize(size, size).png().toBuffer();
    buffers.set(size, png);
    fs.writeFileSync(path.join(output, `logo-app-${size}.png`), png);
  }
  const layers = [16,24,32,48,64,128,256];
  const header = Buffer.alloc(6 + layers.length * 16);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(layers.length, 4);
  let offset = header.length;
  layers.forEach((size, index) => {
    const start = 6 + index * 16;
    const png = buffers.get(size);
    header[start] = header[start + 1] = size === 256 ? 0 : size;
    header.writeUInt16LE(1, start + 4);
    header.writeUInt16LE(32, start + 6);
    header.writeUInt32LE(png.length, start + 8);
    header.writeUInt32LE(offset, start + 12);
    offset += png.length;
  });
  fs.writeFileSync(path.join(output, 'zony.ico'), Buffer.concat([header, ...layers.map(s => buffers.get(s))]));
  console.log(`Exported ${sizes.length} PNG sizes and ${layers.length} ICO layers.`);
}
main().catch(error => { console.error(error); process.exitCode = 1; });
