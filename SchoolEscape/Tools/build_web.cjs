// Produce a self-contained, offline-playable review build. No Unity license,
// web server, CDN or file:// fetch permission is required to play this artifact.
const fs=require('node:fs'),path=require('node:path');
const esbuild=require(process.env.ESBUILD_MODULE||'/workspace/escape-tools/node_modules/esbuild');
const root=path.resolve(__dirname,'..'),web=path.join(root,'Web');
const destination=path.resolve(process.argv[2]||path.join(root,'Builds/Browser/index.html'));
(async()=>{
 const source=fs.readFileSync(path.join(web,'game.js'),'utf8');
 const lines=source.split('\n');const imports=[];while(lines[0]?.startsWith('import '))imports.push(lines.shift());
 const result=await esbuild.build({stdin:{contents:imports.join('\n')+'\n(async()=>{\n'+lines.join('\n')+'\n})()',resolveDir:web,sourcefile:'game.js'},bundle:true,write:false,format:'iife',target:'es2020',minify:true,legalComments:'inline'});
 const resources=path.join(root,'Assets/Resources');
 const data=(name,type)=>`data:${type};base64,${fs.readFileSync(path.join(resources,name)).toString('base64')}`;
 const assets={map:JSON.parse(fs.readFileSync(path.join(resources,'SchoolMap.json'),'utf8')),
  '../Assets/Resources/FloorTexture.png':data('FloorTexture.png','image/png'),
  '../Assets/Resources/WallTexture.png':data('WallTexture.png','image/png')};
 const css=fs.readFileSync(path.join(web,'style.css'),'utf8').replace('../Assets/Resources/SchoolFont.otf',data('SchoolFont.otf','font/otf'));
 const script=('globalThis.SCHOOL_ESCAPE_ASSETS='+JSON.stringify(assets)+';\n'+result.outputFiles[0].text).replace(/<\/script/gi,'<\\/script');
 let html=fs.readFileSync(path.join(web,'index.html'),'utf8').replace('<link rel="stylesheet" href="style.css">',()=>'<style>'+css+'</style>')
  .replace('<script type="module" src="game.js"></script>',()=>'<script>'+script+'</script>');
 const escape=s=>s.replace(/&/g,'&amp;').replace(/</g,'&lt;');
 const licenses=fs.readFileSync(path.join(web,'vendor/THREE-LICENSE.txt'),'utf8')+'\n\n'+fs.readFileSync(path.join(root,'Docs/FontLicense.txt'),'utf8');
 html=html.replace('</body>',()=>'<script type="text/plain" id="third-party-licenses">'+escape(licenses)+'</script></body>');
 fs.mkdirSync(path.dirname(destination),{recursive:true});fs.writeFileSync(destination,html);
 console.log(`Built ${destination} (${Buffer.byteLength(html)} bytes)`);
})().catch(e=>{console.error(e);process.exitCode=1});
