import vert from './generalVert.vs';
import frag from './testFrag.fs';
import { mat4 } from 'gl-matrix';

var lastX, lastY, mouseDown = false;
var yawAngle = 0, pitchAngle = 0;
var rotMatrix = mat4.create();
var _Matrix = mat4.create();
var zoom = 2;
var time = 0;

var canvas = document.getElementById("canvas");
canvas.width = canvas.clientWidth;
canvas.height = canvas.clientHeight;
var gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
var vShader = gl.createShader(gl.VERTEX_SHADER);
var fShader = gl.createShader(gl.FRAGMENT_SHADER);
gl.shaderSource(vShader, vert);
gl.shaderSource(fShader, frag);
gl.compileShader(vShader);
gl.compileShader(fShader);
var compilationLog = gl.getShaderInfoLog(fShader);
console.log('Shader compiler log: ' + compilationLog);

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
var camera_rotation = gl.getUniformLocation(program, "camera_rotation");
var zoom_location = gl.getUniformLocation(program, "zoom");
var time_location = gl.getUniformLocation(program, "time");
gl.uniform2f(resolution_location, canvas.width, canvas.height);
var mx = Math.max(canvas.width, canvas.height);
gl.uniform2f(screen_ratio_location, canvas.width / mx, canvas.height / mx);

var buffer = gl.createBuffer();
gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1.0, -1.0, 1.0, -1.0, -1.0, 1.0, -1.0, 1.0, 1.0, -1.0, 1.0, 1.0]), gl.STATIC_DRAW);
gl.enableVertexAttribArray(position_location);
gl.vertexAttribPointer(position_location, 2, gl.FLOAT, false, 0, 0);

canvas.onmousedown = function (event) {
    var x = event.clientX;
    var y = event.clientY;
    var rect = event.target.getBoundingClientRect();
    if (rect.left <= x && rect.right > x &&
        rect.top <= y && rect.bottom > y) {
        lastX = x;
        lastY = y;
        mouseDown = true;
    }
}
canvas.onmouseup = function (event) {
    mouseDown = false;
}
function degToRad(d) {
    return d * Math.PI / 180;
}
canvas.onmousemove = function (event) {
    var x = event.clientX;
    var y = event.clientY;
    if (mouseDown) {
        yawAngle += (x - lastX) / canvas.width * 400;
        //yawAngle = Math.max(Math.min(yawAngle, maxYawAngle), minYawAngle); 
        pitchAngle += (y - lastY) / canvas.height * 400;
        pitchAngle = Math.max(Math.min(pitchAngle, 89.9), -89.9);
        mat4.rotate(rotMatrix, _Matrix, degToRad(-yawAngle), [0, 1, 0]);

        mat4.rotate(rotMatrix, rotMatrix, degToRad(-pitchAngle), [0, 0, 1]);
    }
    lastX = x;
    lastY = y;
}
canvas.onmousewheel = function (event){
    zoom = zoom - event.wheelDelta/1000;
    zoom = Math.max(Math.min(zoom, 10), .5);
}
function render() {
    time++;
    requestAnimationFrame(render, canvas);
    gl.uniform1f(zoom_location, zoom);
    gl.uniform1f(time_location, time);
    gl.uniformMatrix4fv(camera_rotation, false, rotMatrix);
    gl.drawArrays(gl.TRIANGLES, 0, 6);
}
render();