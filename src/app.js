import vert from './generalVert.vs';
import frag from './mandleFrag.fs';
import { vec3 } from 'gl-matrix';

var lastX, lastY, mouseDown = false;
var yawAngle = 0, pitchAngle = 0;
var playerPos = vec3.fromValues(0, 0, -4);
var time = 0;
var move = [0, 0, 0];
var speed = 3;

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
var playerPos_location = gl.getUniformLocation(program, "playerPos");
var playerFwd_location = gl.getUniformLocation(program, "playerFwd");
var playerUp_location = gl.getUniformLocation(program, "playerUp");
var playerRight_location = gl.getUniformLocation(program, "playerRight");
var time_location = gl.getUniformLocation(program, "time");
var fr_location = gl.getUniformLocation(program, "fixed_radius");
var mr_location = gl.getUniformLocation(program, "min_radius");
var fl_location = gl.getUniformLocation(program, "folding_limit");
var scale_location = gl.getUniformLocation(program, "scale");
var shadow_location = gl.getUniformLocation(program, "shadow_count");
var detail_location = gl.getUniformLocation(program, "detail");
gl.uniform1f(fr_location, 1.9);
gl.uniform1f(mr_location, .1);
gl.uniform1f(fl_location, 1);
gl.uniform1f(scale_location, -2.8);
gl.uniform1f(detail_location, Math.pow(10, -4));

gl.uniform2f(resolution_location, canvas.width, canvas.height);
gl.uniform1i(shadow_location, 40);
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
        pitchAngle += (y - lastY) / canvas.height * 400;
        pitchAngle = Math.max(Math.min(pitchAngle, 89.999), -89.999);
    }
    lastX = x;
    lastY = y;
}

var speed_range = document.getElementById("speed");
canvas.onmousewheel = function (event) {
    speed += Math.sign(event.wheelDelta);
    speed = Math.max(Math.min(speed, 20), -40);
    speed_range.value = Math.round(speed);
}
speed_range.addEventListener('input', function () {
    speed = parseInt(speed_range.value);
});
document.getElementById("fixed_radius").addEventListener('input', function () {
    gl.uniform1f(fr_location, parseFloat(document.getElementById("fixed_radius").value));
});
document.getElementById("min_radius").addEventListener('input', function () {
    gl.uniform1f(mr_location, parseFloat(document.getElementById("min_radius").value));
});
document.getElementById("folding_limit").addEventListener('input', function () {
    gl.uniform1f(fl_location, parseFloat(document.getElementById("folding_limit").value));
});
document.getElementById("scale").addEventListener('input', function () {
    gl.uniform1f(scale_location, parseFloat(document.getElementById("scale").value));
});
document.getElementById("detail").addEventListener('input', function () {
    gl.uniform1f(detail_location, Math.pow(10, -parseFloat(document.getElementById("detail").value)));
});
document.getElementById("shadow").addEventListener('input', function () {
    gl.uniform1i(shadow_location, parseInt(document.getElementById("shadow").value));
});

window.addEventListener("keydown", onKeyDown, false);
window.addEventListener("keyup", onKeyUp, false);

function onKeyDown(event) {
    var keyCode = event.keyCode;
    switch (keyCode) {
        case 68: //d
            move[0] = 1;
            break;
        case 83: //s
            move[2] = -1;
            break;
        case 65: //a
            move[0] = -1;
            break;
        case 87: //w
            move[2] = 1;
            break;
    }
}

function onKeyUp(event) {
    var keyCode = event.keyCode;

    switch (keyCode) {
        case 68: //d
            move[0] = 0;
            break;
        case 83: //s
            move[2] = 0;
            break;
        case 65: //a
            move[0] = 0;
            break;
        case 87: //w
            move[2] = 0;
            break;
    }
}

function movement(deltaTime) {

    var fwd = vec3.fromValues(0, 0, 1);
    vec3.rotateX(fwd, fwd, [0, 0, 0], degToRad(-pitchAngle));
    vec3.rotateY(fwd, fwd, [0, 0, 0], degToRad(-yawAngle));
    var right = vec3.fromValues(0, 0, 0);
    var up = vec3.fromValues(0, 1, 0)
    vec3.cross(right, up, fwd);
    vec3.normalize(right, right);
    vec3.cross(up, right, fwd);
    vec3.normalize(up, up);


    var rightMove = vec3.create();
    var rSpeed = Math.pow(10, speed / 10);
    vec3.scale(rightMove, right, move[0] * deltaTime * rSpeed);
    var fwdMove = vec3.create();
    vec3.scale(fwdMove, fwd, move[2] * deltaTime * rSpeed);
    vec3.add(playerPos, playerPos, rightMove);
    vec3.add(playerPos, playerPos, fwdMove);

    gl.uniform3f(playerUp_location, up[0], up[1], up[2]);
    gl.uniform3f(playerRight_location, right[0], right[1], right[2]);
    gl.uniform3f(playerFwd_location, fwd[0], fwd[1], fwd[2]);
    gl.uniform3f(playerPos_location, playerPos[0], playerPos[1], playerPos[2]);
}



var start = Date.now();
var lastFrame = start;
function render() {
    var current = Date.now();
    var elapsed = current - lastFrame;
    lastFrame = current;
    gl.uniform1f(time_location, current - start);
    movement(elapsed / 1000);

    gl.drawArrays(gl.TRIANGLES, 0, 6);

    requestAnimationFrame(render, canvas);
}
render();