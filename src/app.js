import vert from './generalVert.vs';
import frag from './testFrag.vs';

var canvas = document.getElementById("canvas");
var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
var vShader = gl.createShader(gl.VERTEX_SHADER);
var fShader = gl.createShader(gl.FRAGMENT_SHADER);
gl.shaderSource(vShader, vert);
gl.shaderSource(fShader, frag);
gl.compileShader(vShader);
gl.compileShader(fShader);

var program = gl.createProgram();
gl.attachShader(program, vShader);
gl.attachShader(program, fShader);
gl.linkProgram(program);
gl.deleteShader(vShader);
gl.deleteShader(fShader);
gl.useProgram(program);

var screen_ratio_location = gl.getUniformLocation(program, "screen_ratio");
var position_location = gl.getAttribLocation(program, "position");
var resolution_location = gl.getUniformLocation(program, "resolution");
gl.uniform2f(resolution_location, canvas.width, canvas.height);
var mx = Math.max(canvas.width, canvas.height);
gl.uniform2f(screen_ratio_location, canvas.width / mx, canvas.height / mx);


var buffer = gl.createBuffer();
gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
    -1.0, -1.0,
    1.0, -1.0,
    -1.0, 1.0,
    -1.0, 1.0,
    1.0, -1.0,
    1.0, 1.0]), gl.STATIC_DRAW);


gl.enableVertexAttribArray(position_location);
gl.vertexAttribPointer(position_location, 2, gl.FLOAT, false, 0, 0);

function render() {
    requestAnimationFrame(render, canvas);
    gl.drawArrays(gl.TRIANGLES, 0, 6);
}
render();