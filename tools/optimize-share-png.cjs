// Re-encode PNG bytes without changing decoded RGBA pixels or dimensions.
// Requires sharp and pngjs from the configured Node dependency runtime.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('sharp');
const { PNG } = require('pngjs');

async function main() {
  const [source, destination] = process.argv.slice(2);
  if (!source || !destination || path.resolve(source) === path.resolve(destination))
    throw new Error('Provide distinct source and destination paths.');
  if (fs.existsSync(destination)) throw new Error('Destination already exists.');
  const original = fs.readFileSync(source);
  const decoded = PNG.sync.read(original);
  let best = original;
  let selected = 'original';
  const opaque = decoded.data.every((value, index) => index % 4 !== 3 || value === 255);
  for (const adaptiveFiltering of [false, true]) {
    let encoder = sharp(original);
    if (opaque) encoder = encoder.removeAlpha();
    const candidate = await encoder.png({ compressionLevel: 9, adaptiveFiltering, palette: false, effort: 10 }).toBuffer();
    const check = PNG.sync.read(candidate);
    if (check.width !== decoded.width || check.height !== decoded.height || !check.data.equals(decoded.data))
      throw new Error('Lossless verification failed.');
    if (candidate.length < best.length) { best = candidate; selected = `adaptiveFiltering=${adaptiveFiltering}`; }
  }
  fs.mkdirSync(path.dirname(destination), { recursive: true });
  fs.writeFileSync(destination, best, { flag: 'wx' });
  const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
  console.log(JSON.stringify({sourceBytes:original.length,outputBytes:best.length,savedBytes:original.length-best.length,
    width:decoded.width,height:decoded.height,opaque,selected,rgbaIdentical:true,sourceSha256:sha(original),outputSha256:sha(best)},null,2));
}
main().catch(e => { console.error(e.message); process.exitCode = 1; });
