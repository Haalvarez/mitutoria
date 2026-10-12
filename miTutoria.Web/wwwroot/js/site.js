// custom cursor
const cursor=document.getElementById('cursor');
const ring=document.getElementById('cursorRing');
let mx=0,my=0,rx=0,ry=0;
document.addEventListener('mousemove',e=>{mx=e.clientX;my=e.clientY;cursor.style.left=mx+'px';cursor.style.top=my+'px'});
function animRing(){rx+=(mx-rx)*0.12;ry+=(my-ry)*0.12;ring.style.left=rx+'px';ring.style.top=ry+'px';requestAnimationFrame(animRing)}
animRing();
document.querySelectorAll('button,a,.btn-main,.btn-text').forEach(el=>{
  el.addEventListener('mouseenter',()=>{cursor.style.width='18px';cursor.style.height='18px';ring.style.width='56px';ring.style.height='56px'});
  el.addEventListener('mouseleave',()=>{cursor.style.width='10px';cursor.style.height='10px';ring.style.width='36px';ring.style.height='36px'});
});

// scroll reveal
const reveals=document.querySelectorAll('.reveal');
const obs=new IntersectionObserver((entries)=>{
  entries.forEach(e=>{if(e.isIntersecting){e.target.classList.add('visible');obs.unobserve(e.target)}});
},{threshold:0.12});
reveals.forEach(r=>obs.observe(r));

// Achica la foto en el celular (lado largo 2000px, JPEG): de ~5 MB a ~300 KB, sin perder legibilidad.
// Si el navegador no puede decodificarla (p. ej. HEIC), manda la original.
async function compressImage(file) {
    try {
        const bmp = await createImageBitmap(file, { imageOrientation: 'from-image' });
        const scale = Math.min(1, 2000 / Math.max(bmp.width, bmp.height));
        const canvas = document.createElement('canvas');
        canvas.width = Math.round(bmp.width * scale);
        canvas.height = Math.round(bmp.height * scale);
        canvas.getContext('2d').drawImage(bmp, 0, 0, canvas.width, canvas.height);
        bmp.close?.();
        const blob = await new Promise(r => canvas.toBlob(r, 'image/jpeg', 0.85));
        return blob ? new File([blob], 'pagina.jpg', { type: 'image/jpeg' }) : file;
    } catch {
        return file;
    }
}
